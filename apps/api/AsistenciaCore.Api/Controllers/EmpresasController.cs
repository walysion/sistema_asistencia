using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize(Roles = "SUPER_ADMIN")]
[ApiController]
[Route("api/[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public EmpresasController(ApplicationDbContext context)
    {
        _context = context;
    }

    private string ObtenerUsuarioDelToken()
    {
        return User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "SUPER_ADMIN";
    }

    // GET: api/Empresas (Listado global de empresas registradas en la plataforma SaaS)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetEmpresas()
    {
        var empresas = await _context.Empresas
            .Select(e => new
            {
                e.Id,
                e.RazonSocial,
                e.DocumentoIdentidad,
                e.FechaCreacion,
                TotalEmpleados = _context.Empleados.Count(emp => emp.EmpresaId == e.Id),
                TotalUsuarios = _context.Usuarios.Count(u => u.EmpresaId == e.Id),
                TotalTerminales = _context.TerminalesKiosco.Count(t => t.EmpresaId == e.Id)
            })
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();

        return Ok(empresas);
    }

    // GET: api/Empresas/5 (Detalle de una empresa)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmpresaPorId(int id)
    {
        var empresa = await _context.Empresas
            .FirstOrDefaultAsync(e => e.Id == id);

        if (empresa == null)
        {
            return NotFound("Empresa no encontrada.");
        }

        var detalle = new
        {
            empresa.Id,
            empresa.RazonSocial,
            empresa.DocumentoIdentidad,
            empresa.FechaCreacion,
            TotalEmpleados = await _context.Empleados.CountAsync(emp => emp.EmpresaId == id),
            TotalUsuarios = await _context.Usuarios.CountAsync(u => u.EmpresaId == id),
            TotalTerminales = await _context.TerminalesKiosco.CountAsync(t => t.EmpresaId == id),
            TotalGeocercas = await _context.Geocercas.CountAsync(g => g.EmpresaId == id)
        };

        return Ok(detalle);
    }

    // POST: api/Empresas (Dar de alta una nueva empresa SaaS)
    [HttpPost]
    public async Task<ActionResult<Empresa>> CrearEmpresa([FromBody] CrearEmpresaDto dto)
    {
        bool existeDocumento = await _context.Empresas
            .AnyAsync(e => e.DocumentoIdentidad == dto.DocumentoIdentidad.Trim());

        if (existeDocumento)
        {
            return BadRequest($"Ya existe una empresa registrada con el documento '{dto.DocumentoIdentidad}'.");
        }

        var empresa = new Empresa
        {
            RazonSocial = dto.RazonSocial.Trim(),
            DocumentoIdentidad = dto.DocumentoIdentidad.Trim(),
            FechaCreacion = DateTime.UtcNow
        };

        _context.Empresas.Add(empresa);
        await _context.SaveChangesAsync();

        // Crear automáticamente un Turno Ordinario por defecto para la nueva empresa
        var turnoDefecto = new Turno
        {
            Nombre = "Jornada Ordinaria",
            HoraEntrada = new TimeSpan(9, 0, 0),
            HoraSalida = new TimeSpan(18, 0, 0),
            ToleranciaMinutos = 15,
            MinutosColacion = 60,
            EmpresaId = empresa.Id
        };
        _context.Turnos.Add(turnoDefecto);

        // Registro de auditoría del sistema
        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresa.Id,
            Accion = "ALTA_NUEVA_EMPRESA_SAAS",
            Detalle = $"Empresa '{empresa.RazonSocial}' ({empresa.DocumentoIdentidad}) incorporada al SaaS.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEmpresaPorId), new { id = empresa.Id }, empresa);
    }

    // PUT: api/Empresas/5 (Actualizar datos de la empresa)
    [HttpPut("{id}")]
    public async Task<IActionResult> ActualizarEmpresa(int id, [FromBody] ActualizarEmpresaDto dto)
    {
        var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == id);
        if (empresa == null)
        {
            return NotFound("Empresa no encontrada.");
        }

        empresa.RazonSocial = dto.RazonSocial.Trim();
        empresa.DocumentoIdentidad = dto.DocumentoIdentidad.Trim();

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresa.Id,
            Accion = "ACTUALIZAR_EMPRESA_SAAS",
            Detalle = $"Datos de la empresa #{id} actualizados. Nueva Razón Social: '{empresa.RazonSocial}'.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Empresa actualizada correctamente.", Empresa = empresa });
    }
}

public record CrearEmpresaDto(string RazonSocial, string DocumentoIdentidad);
public record ActualizarEmpresaDto(string RazonSocial, string DocumentoIdentidad);