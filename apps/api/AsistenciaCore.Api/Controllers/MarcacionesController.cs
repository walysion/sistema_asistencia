using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MarcacionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MarcacionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // GET: api/Marcaciones/empleado/1
    [HttpGet("empleado/{empleadoId}")]
    public async Task<ActionResult<IEnumerable<Marcacion>>> GetMarcacionesPorEmpleado(int empleadoId)
    {
        int empresaId = ObtenerEmpresaIdDelToken();

        var empleado = await _context.Empleados
            .FirstOrDefaultAsync(e => e.Id == empleadoId && e.EmpresaId == empresaId);

        if (empleado == null)
        {
            return NotFound("Empleado no encontrado o no pertenece a tu empresa.");
        }

        return await _context.Marcaciones
            .Where(m => m.EmpleadoId == empleadoId)
            .OrderByDescending(m => m.FechaHoraServidor)
            .ToListAsync();
    }

    // POST: api/Marcaciones
    [HttpPost]
    public async Task<ActionResult<Marcacion>> RegistrarMarcacion(Marcacion marcacion)
    {
        int empresaId = ObtenerEmpresaIdDelToken();

        // Cargar al empleado incluyendo su Turno asignado
        var empleado = await _context.Empleados
            .Include(e => e.Turno)
            .FirstOrDefaultAsync(e => e.Id == marcacion.EmpleadoId && e.EmpresaId == empresaId);

        if (empleado == null || !empleado.Activo)
        {
            return BadRequest("El empleado no existe, está inactivo o no pertenece a tu empresa.");
        }

        marcacion.FechaHoraServidor = DateTime.UtcNow;

        // Cálculo de Atrasos automático si el movimiento es 'ENTRADA' y tiene Turno asignado
        if (marcacion.TipoMovimiento.ToUpper() == "ENTRADA" && empleado.Turno != null)
        {
            TimeSpan horaMarcada = marcacion.FechaHoraDispositivo.TimeOfDay;
            TimeSpan horaEntradaOficial = empleado.Turno.HoraEntrada;
            TimeSpan horaLimiteTolerancia = horaEntradaOficial.Add(TimeSpan.FromMinutes(empleado.Turno.ToleranciaMinutos));

            if (horaMarcada > horaLimiteTolerancia)
            {
                TimeSpan tiempoDiferencia = horaMarcada - horaEntradaOficial;
                marcacion.MinutosAtraso = (int)Math.Ceiling(tiempoDiferencia.TotalMinutes);
            }
            else
            {
                marcacion.MinutosAtraso = 0;
            }
        }

        _context.Marcaciones.Add(marcacion);
        await _context.SaveChangesAsync();

        return Ok(marcacion);
    }
}