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

    [HttpGet("resumen-hoy")]
    public async Task<IActionResult> ObtenerResumenHoy()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido o expirado.");

        string cacheKey = $"dashboard_resumen_empresa_{empresaId}_{DateTime.UtcNow:yyyyMMdd}";

        // 1. Intentar obtener datos desde Redis Cache
        var resumenEnCache = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(resumenEnCache))
        {
            var datosCache = JsonSerializer.Deserialize<object>(resumenEnCache);
            return Ok(datosCache);
        }

        // 2. Si no está en caché, consultar PostgreSQL
        var inicioHoyUtc = DateTime.UtcNow.Date;
        var finHoyUtc = inicioHoyUtc.AddDays(1);

        var empleadosEmpresaIds = await _context.Empleados
            .Where(e => e.EmpresaId == empresaId && e.Activo)
            .Select(e => e.Id)
            .ToListAsync();

        int totalEmpleados = empleadosEmpresaIds.Count;

        var marcacionesHoy = await _context.Marcaciones
            .Where(m => empleadosEmpresaIds.Contains(m.EmpleadoId) 
                     && m.FechaHoraServidor >= inicioHoyUtc 
                     && m.FechaHoraServidor < finHoyUtc)
            .ToListAsync();

        int presentes = marcacionesHoy
            .Where(m => m.TipoMovimiento.ToUpper() == "ENTRADA")
            .Select(m => m.EmpleadoId)
            .Distinct()
            .Count();

        int ausentes = totalEmpleados - presentes;
        if (ausentes < 0) ausentes = 0;

        int alertasGeocerca = marcacionesHoy.Count(m => m.FueraDeGeocerca);

        var resultado = new
        {
            FechaConsultaUtc = inicioHoyUtc.ToString("yyyy-MM-dd"),
            TotalEmpleadosActivos = totalEmpleados,
            EmpleadosPresentes = presentes,
            EmpleadosAusentes = ausentes,
            AlertasFueraDeGeocerca = alertasGeocerca,
            TotalMarcacionesRegistradas = marcacionesHoy.Count,
            OrigenDatos = "Base de Datos (PostgreSQL)"
        };

        // 3. Guardar en Redis con expiración automática de 30 segundos
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
        };

        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(resultado), cacheOptions);

        return Ok(resultado);
    }
}