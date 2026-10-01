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

    // GET: api/Turnos (Lista los turnos de la empresa autenticada)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Turno>>> GetTurnos()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        return await _context.Turnos
            .Where(t => t.EmpresaId == empresaId)
            .ToListAsync();
    }

    // POST: api/Turnos (Crea un nuevo turno configurado)
    [HttpPost]
    public async Task<ActionResult<Turno>> CrearTurno(Turno turno)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        turno.EmpresaId = empresaId;

        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTurnos), new { id = turno.Id }, turno);
    }
}