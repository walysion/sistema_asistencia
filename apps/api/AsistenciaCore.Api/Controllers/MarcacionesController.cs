using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Hubs;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MarcacionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<AsistenciaHub> _hubContext;

    public MarcacionesController(ApplicationDbContext context, IHubContext<AsistenciaHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // GET: api/Marcaciones/empleado/1
    [HttpGet("empleado/{empleadoId}")]
    public async Task<ActionResult<IEnumerable<Marcacion>>> GetMarcacionesPorEmpleado(int empleadoId)
    {
        int empresaId = ObtenerEmpresaIdDelToken();

        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.Id == empleadoId && e.EmpresaId == empresaId);

        if (empleado == null)
        {
            return NotFound("Empleado no encontrado o no pertenece a tu empresa.");
        }

        return await _context.Marcaciones
            .Where(m => m.EmpleadoId == empleadoId)
            .OrderByDescending(m => m.FechaHoraServidor)
            .ToListAsync();
    }

    // POST: api/Marcaciones
    [HttpPost]
    public async Task<ActionResult<Marcacion>> RegistrarMarcacion(Marcacion marcacion)
    {
        int empresaId = ObtenerEmpresaIdDelToken();

        var empleado = await _context.Empleados
            .Include(e => e.Turno)
            .FirstOrDefaultAsync(e => e.Id == marcacion.EmpleadoId && e.EmpresaId == empresaId);

        if (empleado == null || !empleado.Activo)
        {
            return BadRequest("El empleado no existe, está inactivo o no pertenece a tu empresa.");
        }

        marcacion.FechaHoraServidor = DateTime.UtcNow;

        // 1. Cálculo de Atrasos automático
        if (marcacion.TipoMovimiento.ToUpper() == "ENTRADA" && empleado.Turno != null)
        {
            TimeSpan horaMarcada = marcacion.FechaHoraDispositivo.TimeOfDay;
            TimeSpan horaEntradaOficial = empleado.Turno.HoraEntrada;
            TimeSpan horaLimiteTolerancia = horaEntradaOficial.Add(TimeSpan.FromMinutes(empleado.Turno.ToleranciaMinutos));

            if (horaMarcada > horaLimiteTolerancia)
            {
                TimeSpan tiempoDiferencia = horaMarcada - horaEntradaOficial;
                marcacion.MinutosAtraso = (int)Math.Ceiling(tiempoDiferencia.TotalMinutes);
            }
            else
            {
                marcacion.MinutosAtraso = 0;
            }
        }

        // 2. Validación de Geocercas (Geofencing)
        var geocercasActivas = await _context.Geocercas
            .Where(g => g.EmpresaId == empresaId && g.Activa)
            .ToListAsync();

        if (geocercasActivas.Any() && marcacion.Latitud.HasValue && marcacion.Longitud.HasValue)
        {
            bool estaDentroDeAlguna = false;

            foreach (var g in geocercasActivas)
            {
                double distanciaMetros = CalcularDistanciaHaversine(
                    marcacion.Latitud.Value, marcacion.Longitud.Value,
                    g.Latitud, g.Longitud
                );

                if (distanciaMetros <= g.RadioMetros)
                {
                    estaDentroDeAlguna = true;
                    break;
                }
            }

            marcacion.FueraDeGeocerca = !estaDentroDeAlguna;
        }

        _context.Marcaciones.Add(marcacion);
        await _context.SaveChangesAsync();

        // 3. Emitir evento WebSockets en tiempo real a la empresa correspondiente
        await _hubContext.Clients.Group($"Empresa_{empresaId}")
            .SendAsync("RecibirMarcacionEnVivo", new
            {
                MarcacionId = marcacion.Id,
                EmpleadoId = marcacion.EmpleadoId,
                NombreEmpleado = empleado.NombreCompleto,
                CodigoTrabajador = empleado.CodigoTrabajador,
                FechaHora = marcacion.FechaHoraServidor.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                TipoMovimiento = marcacion.TipoMovimiento,
                Origen = marcacion.Origen,
                MinutosAtraso = marcacion.MinutosAtraso,
                FueraDeGeocerca = marcacion.FueraDeGeocerca
            });

        return Ok(marcacion);
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