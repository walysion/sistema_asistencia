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

    // GET: api/Geocercas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Geocerca>>> GetGeocercas()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        return await _context.Geocercas
            .Where(g => g.EmpresaId == empresaId)
            .ToListAsync();
    }

    // POST: api/Geocercas
    [HttpPost]
    public async Task<ActionResult<Geocerca>> CrearGeocerca(Geocerca geocerca)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        geocerca.EmpresaId = empresaId;

        _context.Geocercas.Add(geocerca);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGeocercas), new { id = geocerca.Id }, geocerca);
    }
}