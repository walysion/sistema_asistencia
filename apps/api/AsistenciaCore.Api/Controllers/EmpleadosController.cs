using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    private string ObtenerUsuarioDelToken()
    {
        return User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "ADMIN_SISTEMA";
    }

    // GET: api/Empleados (Listar empleados de la empresa)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Empleado>>> GetEmpleados([FromQuery] bool soloActivos = true)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var query = _context.Empleados
            .Include(e => e.Turno)
            .Where(e => e.EmpresaId == empresaId);

        if (soloActivos)
        {
            query = query.Where(e => e.Activo);
        }

        return await query.OrderBy(e => e.NombreCompleto).ToListAsync();
    }

    // POST: api/Empleados (Registrar empleado individual)
    [HttpPost]
    public async Task<ActionResult<Empleado>> CrearEmpleado([FromBody] CrearEmpleadoDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        bool codigoExiste = await _context.Empleados
            .AnyAsync(e => e.EmpresaId == empresaId && e.CodigoTrabajador == dto.CodigoTrabajador);

        if (codigoExiste)
        {
            return BadRequest($"El código de trabajador '{dto.CodigoTrabajador}' ya existe en su empresa.");
        }

        var empleado = new Empleado
        {
            NombreCompleto = dto.NombreCompleto,
            CodigoTrabajador = dto.CodigoTrabajador,
            RutDni = dto.RutDni,
            Cargo = dto.Cargo,
            EmpresaId = empresaId,
            TurnoId = dto.TurnoId,
            Activo = true,
            FechaContratacion = dto.FechaContratacion ?? DateTime.UtcNow
        };

        _context.Empleados.Add(empleado);

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "CREAR_EMPLEADO",
            Detalle = $"Empleado {empleado.NombreCompleto} ({empleado.CodigoTrabajador}) registrado con éxito.",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEmpleados), new { id = empleado.Id }, empleado);
    }

    // POST: api/Empleados/cargar-masivo (Carga masiva vía archivo CSV)
    [HttpPost("cargar-masivo")]
    public async Task<IActionResult> CargarMasivoCsv(IFormFile archivoCsv)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        if (archivoCsv == null || archivoCsv.Length == 0)
        {
            return BadRequest("Debe adjuntar un archivo CSV válido.");
        }

        var listaCodigos = await _context.Empleados
            .Where(e => e.EmpresaId == empresaId)
            .Select(e => e.CodigoTrabajador)
            .ToListAsync();

        var empleadosExistentes = listaCodigos.ToHashSet();

        var turnosEmpresa = await _context.Turnos
            .Where(t => t.EmpresaId == empresaId)
            .ToDictionaryAsync(t => t.Id);

        int turnodefaultId = turnosEmpresa.Keys.FirstOrDefault();

        var nuevosEmpleados = new List<Empleado>();
        var erroresLinea = new List<string>();
        int numeroLinea = 1;

        using (var reader = new StreamReader(archivoCsv.OpenReadStream(), Encoding.UTF8))
        {
            // Leer encabezado
            string? encabezado = await reader.ReadLineAsync();

            while (!reader.EndOfStream)
            {
                numeroLinea++;
                string? linea = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(linea)) continue;

                string[] columnas = linea.Split(';');
                if (columnas.Length < 3)
                {
                    columnas = linea.Split(','); // Reintento con separador por coma
                }

                if (columnas.Length < 3)
                {
                    erroresLinea.Add($"Línea {numeroLinea}: Formato de columnas insuficiente.");
                    continue;
                }

                string codigoTrabajador = columnas[0].Trim().Replace("\"", "");
                string nombreCompleto = columnas[1].Trim().Replace("\"", "");
                string rutDni = columnas[2].Trim().Replace("\"", "");
                string cargo = columnas.Length > 3 ? columnas[3].Trim().Replace("\"", "") : "Operativo";

                if (string.IsNullOrWhiteSpace(codigoTrabajador) || string.IsNullOrWhiteSpace(nombreCompleto))
                {
                    erroresLinea.Add($"Línea {numeroLinea}: Código de trabajador o Nombre vacío.");
                    continue;
                }

                if (empleadosExistentes.Contains(codigoTrabajador) || nuevosEmpleados.Any(e => e.CodigoTrabajador == codigoTrabajador))
                {
                    erroresLinea.Add($"Línea {numeroLinea}: El código '{codigoTrabajador}' ya existe.");
                    continue;
                }

                var emp = new Empleado
                {
                    CodigoTrabajador = codigoTrabajador,
                    NombreCompleto = nombreCompleto,
                    RutDni = rutDni,
                    Cargo = cargo,
                    EmpresaId = empresaId,
                    TurnoId = turnodefaultId > 0 ? turnodefaultId : null,
                    Activo = true,
                    FechaContratacion = DateTime.UtcNow
                };

                nuevosEmpleados.Add(emp);
            }
        }

        if (nuevosEmpleados.Any())
        {
            await _context.Empleados.AddRangeAsync(nuevosEmpleados);

            _context.AuditoriaLogs.Add(new AuditoriaLog
            {
                EmpresaId = empresaId,
                Accion = "CARGA_MASIVA_EMPLEADOS",
                Detalle = $"Se cargaron exitosamente {nuevosEmpleados.Count} empleados mediante archivo CSV.",
                FechaHoraUtc = DateTime.UtcNow,
                Usuario = ObtenerUsuarioDelToken()
            });

            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            Mensaje = $"Procesamiento completado. {nuevosEmpleados.Count} empleados creados.",
            TotalInsertados = nuevosEmpleados.Count,
            TotalErrores = erroresLinea.Count,
            Errores = erroresLinea
        });
    }
}

public record CrearEmpleadoDto(string NombreCompleto, string CodigoTrabajador, string? RutDni, string? Cargo, int? TurnoId, DateTime? FechaContratacion);