using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TerminalesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TerminalesController(ApplicationDbContext context)
    {
        _context = context;
    }

    private int ObtenerEmpresaIdDelToken()
    {
        var empresaIdClaim = User.FindFirst("EmpresaId")?.Value;
        return int.TryParse(empresaIdClaim, out int id) ? id : 0;
    }

    // GET: api/Terminales (Administración Web: listar terminales de la empresa)
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TerminalKiosco>>> GetTerminales()
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        return await _context.TerminalesKiosco
            .Where(t => t.EmpresaId == empresaId)
            .ToListAsync();
    }

    // POST: api/Terminales/registrar (Administración Web: dar de alta un nuevo reloj/tablet)
    [Authorize]
    [HttpPost("registrar")]
    public async Task<ActionResult<TerminalKiosco>> RegistrarTerminal([FromBody] RegistrarTerminalDto dto)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        // Generación de API Key segura de 32 bytes en formato Hexadecimal
        byte[] apiKeyBytes = RandomNumberGenerator.GetBytes(32);
        string apiKeyGenerada = Convert.ToHexString(apiKeyBytes);

        var terminal = new TerminalKiosco
        {
            Nombre = dto.Nombre,
            NumeroSerie = dto.NumeroSerie,
            ApiKey = apiKeyGenerada,
            EmpresaId = empresaId,
            GeocercaId = dto.GeocercaId,
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };

        _context.TerminalesKiosco.Add(terminal);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = "Terminal registrado correctamente. Guarde la ApiKey en el dispositivo.",
            TerminalId = terminal.Id,
            ApiKey = apiKeyGenerada
        });
    }

    // POST: api/Terminales/marcar-kiosco (Marcación pública desde Tablet usando cabecera X-Device-ApiKey)
    [HttpPost("marcar-kiosco")]
    public async Task<IActionResult> MarcarDesdeKiosco([FromHeader(Name = "X-Device-ApiKey")] string? apiKey, [FromBody] MarcacionKioscoDto dto)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Unauthorized("Se requiere la cabecera 'X-Device-ApiKey'.");
        }

        var terminal = await _context.TerminalesKiosco
            .Include(t => t.Geocerca)
            .FirstOrDefaultAsync(t => t.ApiKey == apiKey && t.Activo);

        if (terminal == null)
        {
            return Unauthorized("ApiKey de terminal inválida o dispositivo desactivado.");
        }

        // Cargar empleado por Código de Trabajador o RUT/DNI dentro de la misma empresa
        var empleado = await _context.Empleados
            .Include(e => e.Turno)
            .FirstOrDefaultAsync(e => e.CodigoTrabajador == dto.CodigoTrabajador && e.EmpresaId == terminal.EmpresaId);

        if (empleado == null || !empleado.Activo)
        {
            return BadRequest("Trabajador no encontrado o inactivo.");
        }

        var horaActualUtc = DateTime.UtcNow;
        terminal.UltimaConexion = horaActualUtc;

        var marcacion = new Marcacion
        {
            EmpleadoId = empleado.Id,
            FechaHoraServidor = horaActualUtc,
            FechaHoraDispositivo = DateTime.SpecifyKind(dto.FechaHoraDispositivo, DateTimeKind.Utc),
            TipoMovimiento = string.IsNullOrWhiteSpace(dto.TipoMovimiento) ? "ENTRADA" : dto.TipoMovimiento.ToUpper(),
            Origen = $"KIOSCO_{terminal.Nombre.ToUpper().Replace(" ", "_")}",
            DispositivoId = terminal.NumeroSerie,
            Latitud = dto.Latitud ?? terminal.Geocerca?.Latitud,
            Longitud = dto.Longitud ?? terminal.Geocerca?.Longitud,
            MinutosAtraso = 0,
            FueraDeGeocerca = false
        };

        // Calculation de Atrasos
        if (marcacion.TipoMovimiento == "ENTRADA" && empleado.Turno != null)
        {
            TimeSpan horaMarcada = marcacion.FechaHoraDispositivo.TimeOfDay;
            TimeSpan horaEntradaOficial = empleado.Turno.HoraEntrada;
            TimeSpan horaLimiteTolerancia = horaEntradaOficial.Add(TimeSpan.FromMinutes(empleado.Turno.ToleranciaMinutos));

            if (horaMarcada > horaLimiteTolerancia)
            {
                TimeSpan tiempoDiferencia = horaMarcada - horaEntradaOficial;
                marcacion.MinutosAtraso = (int)Math.Ceiling(tiempoDiferencia.TotalMinutes);
            }
        }

        _context.Marcaciones.Add(marcacion);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Mensaje = $"Marcación de {marcacion.TipoMovimiento} registrada con éxito.",
            Empleado = empleado.NombreCompleto,
            FechaHora = marcacion.FechaHoraServidor.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            AtrasoMinutos = marcacion.MinutosAtraso
        });
    }
}

public record RegistrarTerminalDto(string Nombre, string NumeroSerie, int? GeocercaId);
public record MarcacionKioscoDto(string CodigoTrabajador, DateTime FechaHoraDispositivo, string TipoMovimiento, double? Latitud, double? Longitud);