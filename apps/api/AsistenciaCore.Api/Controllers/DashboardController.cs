using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
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

        var inicioHoyUtc = DateTime.UtcNow.Date;
        var finHoyUtc = inicioHoyUtc.AddDays(1);

        // Obtener IDs de empleados activos pertenecientes a la empresa en sesión
        var empleadosEmpresaIds = await _context.Empleados
            .Where(e => e.EmpresaId == empresaId && e.Activo)
            .Select(e => e.Id)
            .ToListAsync();

        int totalEmpleados = empleadosEmpresaIds.Count;

        // Obtener todas las marcaciones realizadas el día de hoy por el personal de la empresa
        var marcacionesHoy = await _context.Marcaciones
            .Where(m => empleadosEmpresaIds.Contains(m.EmpleadoId) 
                     && m.FechaHoraServidor >= inicioHoyUtc 
                     && m.FechaHoraServidor < finHoyUtc)
            .ToListAsync();

        // Empleados distintos con al menos un marcaje de "ENTRADA" registrado hoy
        int presentes = marcacionesHoy
            .Where(m => m.TipoMovimiento.ToUpper() == "ENTRADA")
            .Select(m => m.EmpleadoId)
            .Distinct()
            .Count();

        int ausentes = totalEmpleados - presentes;
        if (ausentes < 0) ausentes = 0;

        int alertasGeocerca = marcacionesHoy.Count(m => m.FueraDeGeocerca);

        return Ok(new
        {
            FechaConsultaUtc = inicioHoyUtc.ToString("yyyy-MM-dd"),
            TotalEmpleadosActivos = totalEmpleados,
            EmpleadosPresentes = presentes,
            EmpleadosAusentes = ausentes,
            AlertasFueraDeGeocerca = alertasGeocerca,
            TotalMarcacionesRegistradas = marcacionesHoy.Count
        });
    }
}