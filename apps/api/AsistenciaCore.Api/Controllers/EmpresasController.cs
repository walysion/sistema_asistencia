using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmpresasController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Empresas
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Empresa>>> GetEmpresas()
    {
        return await _context.Empresas
            .Include(e => e.Empleados)
            .ToListAsync();
    }

    // POST: api/Empresas
    [HttpPost]
    public async Task<ActionResult<Empresa>> CrearEmpresa(Empresa empresa)
    {
        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEmpresas), new { id = empresa.Id }, empresa);
    }
}