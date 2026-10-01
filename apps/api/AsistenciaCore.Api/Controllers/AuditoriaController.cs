using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AuditoriaController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AuditoriaController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // GET: api/Auditoria
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditoriaLog>>> GetLogs([FromQuery] int limite = 100)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var logs = await _context.AuditoriaLogs
            .Where(a => a.EmpresaId == empresaId)
            .OrderByDescending(a => a.FechaHoraUtc)
            .Take(limite)
            .ToListAsync();

        return Ok(logs);
    }
}