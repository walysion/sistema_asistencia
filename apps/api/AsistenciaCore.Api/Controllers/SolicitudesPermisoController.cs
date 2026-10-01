using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SolicitudesPermisoController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SolicitudesPermisoController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    private string ObtenerUsuarioDelToken()
    {
        return User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "ADMIN_SISTEMA";
    }

    // GET: api/SolicitudesPermiso (Listar solicitudes de la empresa)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SolicitudPermiso>>> GetSolicitudes([FromQuery] string? estado)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var query = _context.SolicitudesPermiso
            .Include(s => s.Empleado)
            .Where(s => s.EmpresaId == empresaId);

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(s => s.Estado.ToUpper() == estado.ToUpper());
        }

        return await query.OrderByDescending(s => s.FechaSolicitud).ToListAsync();
    }

    // POST: api/SolicitudesPermiso (Crear nueva solicitud)
    [HttpPost]
    public async Task<ActionResult<SolicitudPermiso>> CrearSolicitud([FromBody] CrearSolicitudPermisoDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.Id == dto.EmpleadoId && e.EmpresaId == empresaId);

        if (empleado == null || !empleado.Activo)
        {
            return BadRequest("El empleado especificado no existe o no pertenece a tu empresa.");
        }

        if (dto.FechaInicio > dto.FechaFin)
        {
            return BadRequest("La fecha de inicio no puede ser posterior a la fecha de término.");
        }

        var solicitud = new SolicitudPermiso
        {
            EmpleadoId = dto.EmpleadoId,
            EmpresaId = empresaId,
            TipoPermiso = dto.TipoPermiso.ToUpper(), // Ej: "VACACIONES", "LICENCIA_MEDICA", "DIA_ADMINISTRATIVO"
            Motivo = dto.Motivo,
            FechaInicio = DateTime.SpecifyKind(dto.FechaInicio.Date, DateTimeKind.Utc),
            FechaFin = DateTime.SpecifyKind(dto.FechaFin.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc),
            Estado = "PENDIENTE",
            FechaSolicitud = DateTime.UtcNow
        };

        _context.SolicitudesPermiso.Add(solicitud);

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "CREAR_SOLICITUD_PERMISO",
            Detalle = $"Solicitud de {solicitud.TipoPermiso} registrada para {empleado.NombreCompleto} ({dto.FechaInicio:yyyy-MM-dd} al {dto.FechaFin:yyyy-MM-dd}).",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSolicitudes), new { id = solicitud.Id }, solicitud);
    }

    // PUT: api/SolicitudesPermiso/1/aprobar (Aprobar solicitud)
    [HttpPut("{id}/aprobar")]
    public async Task<IActionResult> AprobarSolicitud(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var solicitud = await _context.SolicitudesPermiso
            .Include(s => s.Empleado)
            .FirstOrDefaultAsync(s => s.Id == id && s.EmpresaId == empresaId);

        if (solicitud == null)
        {
            return NotFound("Solicitud de permiso no encontrada.");
        }

        if (solicitud.Estado != "PENDIENTE")
        {
            return BadRequest($"La solicitud ya se encuentra en estado '{solicitud.Estado}'.");
        }

        solicitud.Estado = "APROBADO";

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "APROBAR_SOLICITUD_PERMISO",
            Detalle = $"Solicitud #{solicitud.Id} ({solicitud.TipoPermiso}) APROBADA para {solicitud.Empleado?.NombreCompleto}.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Solicitud aprobada exitosamente.", SolicitudId = solicitud.Id, Estado = solicitud.Estado });
    }

    // PUT: api/SolicitudesPermiso/1/rechazar (Rechazar solicitud)
    [HttpPut("{id}/rechazar")]
    public async Task<IActionResult> RechazarSolicitud(int id, [FromBody] RechazarSolicitudDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var solicitud = await _context.SolicitudesPermiso
            .Include(s => s.Empleado)
            .FirstOrDefaultAsync(s => s.Id == id && s.EmpresaId == empresaId);

        if (solicitud == null)
        {
            return NotFound("Solicitud de permiso no encontrada.");
        }

        if (solicitud.Estado != "PENDIENTE")
        {
            return BadRequest($"La solicitud ya se encuentra en estado '{solicitud.Estado}'.");
        }

        solicitud.Estado = "RECHAZADO";

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "RECHAZAR_SOLICITUD_PERMISO",
            Detalle = $"Solicitud #{solicitud.Id} ({solicitud.TipoPermiso}) RECHAZADA. Motivo: {dto.MotivoRechazo}",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Solicitud rechazada.", SolicitudId = solicitud.Id, Estado = solicitud.Estado });
    }
}

public record CrearSolicitudPermisoDto(int EmpleadoId, string TipoPermiso, string Motivo, DateTime FechaInicio, DateTime FechaFin);
public record RechazarSolicitudDto(string MotivoRechazo);