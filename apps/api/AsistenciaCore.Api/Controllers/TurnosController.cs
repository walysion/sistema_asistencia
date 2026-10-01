using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TurnosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TurnosController(ApplicationDbContext context)
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

    // GET: api/Turnos (Listar todos los turnos de la empresa)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Turno>>> GetTurnos()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        return await _context.Turnos
            .Where(t => t.EmpresaId == empresaId)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
    }

    // GET: api/Turnos/5 (Obtener turno por ID)
    [HttpGet("{id}")]
    public async Task<ActionResult<Turno>> GetTurnoPorId(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var turno = await _context.Turnos
            .FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        if (turno == null) return NotFound("Turno no encontrado.");

        return Ok(turno);
    }

    // POST: api/Turnos (Crear un nuevo turno/horario)
    [HttpPost]
    public async Task<ActionResult<Turno>> CrearTurno([FromBody] CrearTurnoDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        if (dto.ToleranciaMinutos < 0)
        {
            return BadRequest("Los minutos de tolerancia no pueden ser negativos.");
        }

        var turno = new Turno
        {
            Nombre = dto.Nombre,
            HoraEntrada = TimeSpan.Parse(dto.HoraEntrada),
            HoraSalida = TimeSpan.Parse(dto.HoraSalida),
            ToleranciaMinutos = dto.ToleranciaMinutos,
            MinutosColacion = dto.MinutosColacion,
            EmpresaId = empresaId
        };

        _context.Turnos.Add(turno);

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "CREAR_TURNO",
            Detalle = $"Turno '{turno.Nombre}' registrado ({dto.HoraEntrada} a {dto.HoraSalida}, Tolerancia: {dto.ToleranciaMinutos}m).",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTurnoPorId), new { id = turno.Id }, turno);
    }

    // PUT: api/Turnos/5 (Actualizar un turno existente)
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarTurno(int id, [FromBody] ActualizarTurnoDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var turno = await _context.Turnos
            .FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        if (turno == null) return NotFound("Turno no encontrado.");

        turno.Nombre = dto.Nombre;
        turno.HoraEntrada = TimeSpan.Parse(dto.HoraEntrada);
        turno.HoraSalida = TimeSpan.Parse(dto.HoraSalida);
        turno.ToleranciaMinutos = dto.ToleranciaMinutos;
        turno.MinutosColacion = dto.MinutosColacion;

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "ACTUALIZAR_TURNO",
            Detalle = $"Turno #{id} ({turno.Nombre}) modificado correctamente.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Turno actualizado con éxito.", Turno = turno });
    }

    // DELETE: api/Turnos/5 (Eliminar turno si no tiene empleados asignados)
    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarTurno(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var turno = await _context.Turnos
            .FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        if (turno == null) return NotFound("Turno no encontrado.");

        bool tieneEmpleadosAsignados = await _context.Empleados
            .AnyAsync(e => e.TurnoId == id && e.EmpresaId == empresaId);

        if (tieneEmpleadosAsignados)
        {
            return BadRequest("No se puede eliminar el turno porque tiene empleados asociados. Reasigne los empleados antes de borrarlo.");
        }

        _context.Turnos.Remove(turno);

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "ELIMINAR_TURNO",
            Detalle = $"Turno '{turno.Nombre}' (ID: {id}) fue eliminado.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = $"Turno '{turno.Nombre}' eliminado correctamente." });
    }
}

public record CrearTurnoDto(string Nombre, string HoraEntrada, string HoraSalida, int ToleranciaMinutos, int MinutosColacion);
public record ActualizarTurnoDto(string Nombre, string HoraEntrada, string HoraSalida, int ToleranciaMinutos, int MinutosColacion);