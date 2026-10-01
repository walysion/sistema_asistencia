using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SyncController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // POST: api/Sync/marcaciones-offline
    [HttpPost("marcaciones-offline")]
    public async Task<IActionResult> SincronizarMarcacionesOffline([FromBody] SyncBatchDto batchDto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        if (batchDto?.Marcaciones == null || !batchDto.Marcaciones.Any())
        {
            return BadRequest("El paquete de marcaciones está vacío.");
        }

        // Cargar empleados activos de la empresa
        var empleadosEmpresa = await _context.Empleados
            .Include(e => e.Turno)
            .Where(e => e.EmpresaId == empresaId && e.Activo)
            .ToDictionaryAsync(e => e.Id);

        // Cargar geocercas activas
        var geocercasActivas = await _context.Geocercas
            .Where(g => g.EmpresaId == empresaId && g.Activa)
            .ToListAsync();

        int procesadasExito = 0;
        var errores = new List<string>();
        var nuevasMarcaciones = new List<Marcacion>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var item in batchDto.Marcaciones)
            {
                if (!empleadosEmpresa.TryGetValue(item.EmpleadoId, out var empleado))
                {
                    errores.Add($"Empleado ID {item.EmpleadoId} no existe, está inactivo o no pertenece a tu empresa.");
                    continue;
                }

                var marcacion = new Marcacion
                {
                    EmpleadoId = item.EmpleadoId,
                    FechaHoraServidor = DateTime.UtcNow,
                    FechaHoraDispositivo = DateTime.SpecifyKind(item.FechaHoraDispositivo, DateTimeKind.Utc),
                    TipoMovimiento = string.IsNullOrWhiteSpace(item.TipoMovimiento) ? "ENTRADA" : item.TipoMovimiento.ToUpper(),
                    Origen = string.IsNullOrWhiteSpace(item.Origen) ? "APP_MOVIL_OFFLINE" : item.Origen,
                    Latitud = item.Latitud,
                    Longitud = item.Longitud,
                    EsSincronizacionOffline = true,
                    DispositivoId = item.DispositivoId,
                    MinutosAtraso = 0,
                    FueraDeGeocerca = false
                };

                // 1. Cálculo de Atrasos
                if (marcacion.TipoMovimiento == "ENTRADA" && empleado.Turno != null)
                {
                    TimeSpan horaMarcada = marcacion.FechaHoraDispositivo.TimeOfDay;
                    TimeSpan horaEntradaOficial = empleado.Turno.HoraEntrada;
                    TimeSpan horaLimiteTolerancia = horaEntradaOficial.Add(TimeSpan.FromMinutes(empleado.Turno.ToleranciaMinutos));

                    if (horaMarcada > horaLimiteTolerancia)
                    {
                        TimeSpan tiempoDiferencia = horaMarcada - horaEntradaOficial;
                        marcacion.MinutosAtraso = (int)Math.Ceiling(tiempoDiferencia.TotalMinutes);
                    }
                }

                // 2. Validación de Geocercas
                if (geocercasActivas.Any() && marcacion.Latitud.HasValue && marcacion.Longitud.HasValue)
                {
                    bool estaDentro = geocercasActivas.Any(g => 
                        CalcularDistanciaHaversine(marcacion.Latitud.Value, marcacion.Longitud.Value, g.Latitud, g.Longitud) <= g.RadioMetros);

                    marcacion.FueraDeGeocerca = !estaDentro;
                }

                nuevasMarcaciones.Add(marcacion);
                procesadasExito++;
            }

            if (nuevasMarcaciones.Any())
            {
                _context.Marcaciones.AddRange(nuevasMarcaciones);
                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            return Ok(new
            {
                TotalRecibidas = batchDto.Marcaciones.Count,
                ProcesadasConExito = procesadasExito,
                TotalFallidas = errores.Count,
                DetalleErrores = errores
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, $"Error interno al sincronizar el lote offline: {ex.Message}");
        }
    }

    private static double CalcularDistanciaHaversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double RadioTierraMetros = 6371000.0;
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return RadioTierraMetros * c;
    }

    private static double ToRadians(double grados) => grados * (Math.PI / 180.0);
}

public record SyncBatchDto(List<MarcacionOfflineDto> Marcaciones);

public record MarcacionOfflineDto(
    int EmpleadoId,
    DateTime FechaHoraDispositivo,
    string TipoMovimiento,
    string Origen,
    double? Latitud,
    double? Longitud,
    string DispositivoId
);