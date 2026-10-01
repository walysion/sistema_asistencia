using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PermisosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PermisosController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // GET: api/Permisos (Listar todas las solicitudes de la empresa autenticada)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SolicitudPermiso>>> GetPermisos()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        return await _context.SolicitudesPermiso
            .Include(p => p.Empleado)
            .Where(p => p.EmpresaId == empresaId)
            .OrderByDescending(p => p.FechaSolicitud)
            .ToListAsync();
    }

    // POST: api/Permisos (Crear nueva solicitud de licencia/permiso)
    [HttpPost]
    public async Task<ActionResult<SolicitudPermiso>> CrearSolicitud(SolicitudPermiso permiso)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.Id == permiso.EmpleadoId && e.EmpresaId == empresaId);

        if (empleado == null)
        {
            return BadRequest("El empleado no existe o no pertenece a tu empresa.");
        }

        permiso.EmpresaId = empresaId;
        permiso.FechaSolicitud = DateTime.UtcNow;
        permiso.FechaInicio = DateTime.SpecifyKind(permiso.FechaInicio.Date, DateTimeKind.Utc);
        permiso.FechaFin = DateTime.SpecifyKind(permiso.FechaFin.Date, DateTimeKind.Utc);

        _context.SolicitudesPermiso.Add(permiso);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPermisos), new { id = permiso.Id }, permiso);
    }

    // PUT: api/Permisos/1/evaluar (Aprobar o Rechazar una solicitud)
    [HttpPut("{id}/evaluar")]
    public async Task<IActionResult> EvaluarSolicitud(int id, [FromBody] EvaluarPermisoDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();

        var permiso = await _context.SolicitudesPermiso
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);

        if (permiso == null)
        {
            return NotFound("Solicitud de permiso no encontrada.");
        }

        string estadoNormalizado = dto.NuevoEstado.ToUpper();
        if (estadoNormalizado != "APROBADO" && estadoNormalizado != "RECHAZADO")
        {
            return BadRequest("El estado debe ser 'APROBADO' o 'RECHAZADO'.");
        }

        permiso.Estado = estadoNormalizado;
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = $"Solicitud {id} actualizada a estado: {permiso.Estado}", permiso });
    }
}

public record EvaluarPermisoDto(string NuevoEstado);