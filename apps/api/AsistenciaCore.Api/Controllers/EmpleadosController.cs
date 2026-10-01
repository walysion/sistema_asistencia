using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class EmpleadosController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmpleadosController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // GET: api/Empleados (Solo devuelve los empleados de la empresa del usuario autenticado)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Empleado>>> GetEmpleados()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        return await _context.Empleados
            .Where(e => e.EmpresaId == empresaId)
            .ToListAsync();
    }

    // POST: api/Empleados (Asigna automáticamente la empresa del usuario autenticado)
    [HttpPost]
    public async Task<ActionResult<Empleado>> CrearEmpleado(Empleado empleado)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        empleado.EmpresaId = empresaId;

        _context.Empleados.Add(empleado);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEmpleados), new { id = empleado.Id }, empleado);
    }
}