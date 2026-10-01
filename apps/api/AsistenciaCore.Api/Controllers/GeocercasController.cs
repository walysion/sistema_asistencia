using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GeocercasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GeocercasController(ApplicationDbContext context)
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

    // GET: api/Geocercas (Listar geocercas de la empresa)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Geocerca>>> GetGeocercas([FromQuery] bool soloActivas = true)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var query = _context.Geocercas.Where(g => g.EmpresaId == empresaId);

        if (soloActivas)
        {
            query = query.Where(g => g.Activa);
        }

        return await query.OrderBy(g => g.Nombre).ToListAsync();
    }

    // GET: api/Geocercas/5 (Obtener geocerca por ID)
    [HttpGet("{id}")]
    public async Task<ActionResult<Geocerca>> GetGeocercaPorId(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var geocerca = await _context.Geocercas
            .FirstOrDefaultAsync(g => g.Id == id && g.EmpresaId == empresaId);

        if (geocerca == null) return NotFound("Geocerca no encontrada.");

        return Ok(geocerca);
    }

    // POST: api/Geocercas (Crear nueva geocerca)
    [HttpPost]
    public async Task<ActionResult<Geocerca>> CrearGeocerca([FromBody] CrearGeocercaDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        if (dto.RadioMetros <= 0)
        {
            return BadRequest("El radio de la geocerca debe ser mayor a 0 metros.");
        }

        var geocerca = new Geocerca
        {
            Nombre = dto.Nombre,
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
            RadioMetros = dto.RadioMetros,
            Activa = true,
            EmpresaId = empresaId
        };

        _context.Geocercas.Add(geocerca);

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "CREAR_GEOCERCA",
            Detalle = $"Geocerca '{geocerca.Nombre}' registrada con radio de {geocerca.RadioMetros}m en Lat: {geocerca.Latitud}, Lon: {geocerca.Longitud}.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGeocercaPorId), new { id = geocerca.Id }, geocerca);
    }

    // PUT: api/Geocercas/5 (Actualizar geocerca existente)
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarGeocerca(int id, [FromBody] ActualizarGeocercaDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var geocerca = await _context.Geocercas
            .FirstOrDefaultAsync(g => g.Id == id && g.EmpresaId == empresaId);

        if (geocerca == null) return NotFound("Geocerca no encontrada.");

        geocerca.Nombre = dto.Nombre;
        geocerca.Latitud = dto.Latitud;
        geocerca.Longitud = dto.Longitud;
        geocerca.RadioMetros = dto.RadioMetros;
        geocerca.Activa = dto.Activa;

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "ACTUALIZAR_GEOCERCA",
            Detalle = $"Geocerca #{id} ({geocerca.Nombre}) modificada. Estado activo: {geocerca.Activa}.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Geocerca actualizada con éxito.", Geocerca = geocerca });
    }

    // DELETE: api/Geocercas/5 (Desactivar/Eliminar geocerca de manera lógica)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DesactivarGeocerca(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var geocerca = await _context.Geocercas
            .FirstOrDefaultAsync(g => g.Id == id && g.EmpresaId == empresaId);

        if (geocerca == null) return NotFound("Geocerca no encontrada.");

        geocerca.Activa = false;

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "DESACTIVAR_GEOCERCA",
            Detalle = $"Geocerca '{geocerca.Nombre}' (ID: {id}) fue desactivada.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = $"Geocerca '{geocerca.Nombre}' desactivada correctamente." });
    }
}

public record CrearGeocercaDto(string Nombre, double Latitud, double Longitud, double RadioMetros);
public record ActualizarGeocercaDto(string Nombre, double Latitud, double Longitud, double RadioMetros, bool Activa);