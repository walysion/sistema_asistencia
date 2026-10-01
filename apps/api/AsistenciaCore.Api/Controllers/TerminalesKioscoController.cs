using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TerminalesKioscoController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TerminalesKioscoController(ApplicationDbContext context)
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

    // GET: api/TerminalesKiosco (Listar terminales kiosco de la empresa)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TerminalKiosco>>> GetTerminales()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        return await _context.TerminalesKiosco
            .Include(t => t.Geocerca)
            .Where(t => t.EmpresaId == empresaId)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
    }

    // GET: api/TerminalesKiosco/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TerminalKiosco>> GetTerminalPorId(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var terminal = await _context.TerminalesKiosco
            .Include(t => t.Geocerca)
            .FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        if (terminal == null) return NotFound("Terminal Kiosco no encontrado.");

        return Ok(terminal);
    }

    // POST: api/TerminalesKiosco (Registrar un nuevo terminal tablet/kiosco)
    [HttpPost]
    public async Task<ActionResult<TerminalKiosco>> CrearTerminal([FromBody] CrearTerminalKioscoDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        if (dto.GeocercaId.HasValue)
        {
            bool geocercaValida = await _context.Geocercas
                .AnyAsync(g => g.Id == dto.GeocercaId.Value && g.EmpresaId == empresaId);

            if (!geocercaValida)
            {
                return BadRequest("La geocerca asociada no existe o no pertenece a su empresa.");
            }
        }

        string apiKey = GenerarApiKeyUnica();

        var terminal = new TerminalKiosco
        {
            Nombre = dto.Nombre.Trim(),
            ApiKey = apiKey,
            Activo = true,
            GeocercaId = dto.GeocercaId,
            EmpresaId = empresaId,
            UltimaConexionUtc = DateTime.UtcNow
        };

        _context.TerminalesKiosco.Add(terminal);

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "CREAR_TERMINAL_KIOSCO",
            Detalle = $"Terminal '{terminal.Nombre}' registrado. API Key generada.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTerminalPorId), new { id = terminal.Id }, terminal);
    }

    // PUT: api/TerminalesKiosco/5 (Actualizar datos del terminal)
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarTerminal(int id, [FromBody] ActualizarTerminalKioscoDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var terminal = await _context.TerminalesKiosco
            .FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        if (terminal == null) return NotFound("Terminal Kiosco no encontrado.");

        if (dto.GeocercaId.HasValue)
        {
            bool geocercaValida = await _context.Geocercas
                .AnyAsync(g => g.Id == dto.GeocercaId.Value && g.EmpresaId == empresaId);

            if (!geocercaValida)
            {
                return BadRequest("La geocerca asociada no existe o no pertenece a su empresa.");
            }
        }

        terminal.Nombre = dto.Nombre.Trim();
        terminal.GeocercaId = dto.GeocercaId;
        terminal.Activo = dto.Activo;

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "ACTUALIZAR_TERMINAL_KIOSCO",
            Detalle = $"Terminal #{id} ({terminal.Nombre}) actualizado.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Terminal Kiosco actualizado con éxito.", Terminal = terminal });
    }

    // POST: api/TerminalesKiosco/5/regenerar-apikey (Rotación de seguridad de la API Key)
    [HttpPost("{id}/regenerar-apikey")]
    public async Task<IActionResult> RegenerarApiKey(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var terminal = await _context.TerminalesKiosco
            .FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        if (terminal == null) return NotFound("Terminal Kiosco no encontrado.");

        terminal.ApiKey = GenerarApiKeyUnica();

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "REGENERAR_APIKEY_TERMINAL",
            Detalle = $"API Key regenerada para el terminal '{terminal.Nombre}' (ID: {id}).",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "API Key regenerada con éxito.", ApiKey = terminal.ApiKey });
    }

    // DELETE: api/TerminalesKiosco/5 (Desactivar terminal)
    [HttpDelete("{id}")]
    public async Task<IActionResult> DesactivarTerminal(int id)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var terminal = await _context.TerminalesKiosco
            .FirstOrDefaultAsync(t => t.Id == id && t.EmpresaId == empresaId);

        if (terminal == null) return NotFound("Terminal Kiosco no encontrado.");

        terminal.Activo = false;

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "DESACTIVAR_TERMINAL_KIOSCO",
            Detalle = $"Terminal '{terminal.Nombre}' (ID: {id}) fue desactivado.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = $"Terminal '{terminal.Nombre}' desactivado correctamente." });
    }

    private static string GenerarApiKeyUnica()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return "kiosk_" + Convert.ToHexString(bytes).ToLower();
    }
}

public record CrearTerminalKioscoDto(string Nombre, int? GeocercaId);
public record ActualizarTerminalKioscoDto(string Nombre, int? GeocercaId, bool Activo);