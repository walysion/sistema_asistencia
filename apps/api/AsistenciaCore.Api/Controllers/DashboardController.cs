using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using AsistenciaCore.Api.Data;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;

    public DashboardController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // GET: api/Dashboard/resumen-hoy (Optimizado con Redis Cache)
    [HttpGet("resumen-hoy")]
    public async Task<IActionResult> ObtenerResumenHoy()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        string cacheKey = $"dashboard_resumen_{empresaId}_{DateTime.UtcNow:yyyyMMdd_HHmm}";
        string? cachedData = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            var resumenCacheado = JsonSerializer.Deserialize<ResumenDashboardDto>(cachedData);
            return Ok(resumenCacheado);
        }

        var inicioHoyUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var finHoyUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        int totalEmpleados = await _context.Empleados
            .CountAsync(e => e.EmpresaId == empresaId && e.Activo);

        var marcacionesHoy = await _context.Marcaciones
            .Where(m => m.Empleado != null 
                     && m.Empleado.EmpresaId == empresaId 
                     && m.FechaHoraServidor >= inicioHoyUtc 
                     && m.FechaHoraServidor <= finHoyUtc 
                     && m.TipoMovimiento.ToUpper() == "ENTRADA")
            .ToListAsync();

        int presentes = marcacionesHoy.Select(m => m.EmpleadoId).Distinct().Count();
        int conAtraso = marcacionesHoy.Where(m => m.MinutosAtraso > 0).Select(m => m.EmpleadoId).Distinct().Count();
        int fueraDeGeocerca = marcacionesHoy.Where(m => m.FueraDeGeocerca).Select(m => m.EmpleadoId).Distinct().Count();

        int conPermiso = await _context.SolicitudesPermiso
            .Where(p => p.EmpresaId == empresaId 
                     && p.Estado.ToUpper() == "APROBADO" 
                     && p.FechaInicio <= finHoyUtc 
                     && p.FechaFin >= inicioHoyUtc)
            .Select(p => p.EmpleadoId)
            .Distinct()
            .CountAsync();

        int ausentes = Math.Max(0, totalEmpleados - presentes - conPermiso);

        var resumen = new ResumenDashboardDto(
            FechaUtc: DateTime.UtcNow.ToString("yyyy-MM-dd"),
            TotalEmpleados: totalEmpleados,
            Presentes: presentes,
            Ausentes: ausentes,
            ConAtraso: conAtraso,
            ConPermiso: conPermiso,
            FueraDeGeocerca: fueraDeGeocerca,
            PorcentajeAsistencia: totalEmpleados > 0 ? Math.Round((double)presentes / totalEmpleados * 100, 2) : 0
        );

        // Almacenar métricas calculadas en Redis por 60 segundos
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(resumen), cacheOptions);

        return Ok(resumen);
    }

    // GET: api/Dashboard/top-atrasos (Ranking de los empleados con más minutos acumulados de tardanza en el mes)
    [HttpGet("top-atrasos")]
    public async Task<IActionResult> ObtenerTopAtrasosDelMes([FromQuery] int limite = 5)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var inicioMesUtc = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var finMesUtc = inicioMesUtc.AddMonths(1).AddTicks(-1);

        // Se trae la lista filtrada de la BD para procesar el agrupamiento en memoria de forma segura
        var marcacionesConAtraso = await _context.Marcaciones
            .Include(m => m.Empleado)
            .Where(m => m.Empleado != null 
                     && m.Empleado.EmpresaId == empresaId 
                     && m.FechaHoraServidor >= inicioMesUtc 
                     && m.FechaHoraServidor <= finMesUtc 
                     && m.MinutosAtraso > 0)
            .ToListAsync();

        var topAtrasos = marcacionesConAtraso
            .GroupBy(m => m.Empleado!)
            .Select(g => new TopEmpleadoAtrasoDto(
                EmpleadoId: g.Key.Id,
                CodigoTrabajador: g.Key.CodigoTrabajador,
                NombreCompleto: g.Key.NombreCompleto,
                TotalMinutosAtraso: g.Sum(m => m.MinutosAtraso),
                TotalEventosAtraso: g.Count()
            ))
            .OrderByDescending(x => x.TotalMinutosAtraso)
            .Take(limite)
            .ToList();

        return Ok(topAtrasos);
    }

    // GET: api/Dashboard/tendencia-semanal (Métricas de los últimos 7 días para gráficos)
    [HttpGet("tendencia-semanal")]
    public async Task<IActionResult> ObtenerTendenciaSemanal()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var hace7DiasUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-6), DateTimeKind.Utc);
        var hoyUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var marcacionesUltimaSemana = await _context.Marcaciones
            .Where(m => m.Empleado != null 
                     && m.Empleado.EmpresaId == empresaId 
                     && m.FechaHoraServidor >= hace7DiasUtc 
                     && m.FechaHoraServidor <= hoyUtc 
                     && m.TipoMovimiento.ToUpper() == "ENTRADA")
            .ToListAsync();

        var tendencia = marcacionesUltimaSemana
            .GroupBy(m => m.FechaHoraServidor.Date)
            .Select(g => new TendenciaDiariaDto(
                Fecha: g.Key.ToString("yyyy-MM-dd"),
                TotalPresentes: g.Select(m => m.EmpleadoId).Distinct().Count(),
                TotalAtrasos: g.Where(m => m.MinutosAtraso > 0).Select(m => m.EmpleadoId).Distinct().Count(),
                TotalFueraGeocerca: g.Where(m => m.FueraDeGeocerca).Select(m => m.EmpleadoId).Distinct().Count()
            ))
            .OrderBy(t => t.Fecha)
            .ToList();

        return Ok(tendencia);
    }

    // POST: api/Dashboard/limpiar-cache (Fuerza la invalidación de la caché en Redis)
    [HttpPost("limpiar-cache")]
    public async Task<IActionResult> LimpiarCacheDashboard()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        string cacheKey = $"dashboard_resumen_{empresaId}_{DateTime.UtcNow:yyyyMMdd_HHmm}";
        await _cache.RemoveAsync(cacheKey);

        return Ok(new { Mensaje = "Caché de dashboard purgada correctamente." });
    }
}

public record ResumenDashboardDto(
    string FechaUtc,
    int TotalEmpleados,
    int Presentes,
    int Ausentes,
    int ConAtraso,
    int ConPermiso,
    int FueraDeGeocerca,
    double PorcentajeAsistencia
);

public record TopEmpleadoAtrasoDto(
    int EmpleadoId,
    string CodigoTrabajador,
    string NombreCompleto,
    int TotalMinutosAtraso,
    int TotalEventosAtraso
);

public record TendenciaDiariaDto(
    string Fecha,
    int TotalPresentes,
    int TotalAtrasos,
    int TotalFueraGeocerca
);