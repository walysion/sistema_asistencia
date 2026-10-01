using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportesController(ApplicationDbContext context)
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

    // GET: api/Reportes/asistencia-rango?fechaInicio=2026-10-01&fechaFin=2026-10-31
    [HttpGet("asistencia-rango")]
    public async Task<IActionResult> ObtenerReporteRango([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var inicioUtc = DateTime.SpecifyKind(fechaInicio.Date, DateTimeKind.Utc);
        var finUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var reporte = await _context.Marcaciones
            .Include(m => m.Empleado)
            .Where(m => m.Empleado != null 
                     && m.Empleado.EmpresaId == empresaId 
                     && m.FechaHoraServidor >= inicioUtc 
                     && m.FechaHoraServidor <= finUtc)
            .OrderBy(m => m.FechaHoraServidor)
            .Select(m => new
            {
                MarcacionId = m.Id,
                EmpleadoId = m.EmpleadoId,
                NombreEmpleado = m.Empleado!.NombreCompleto,
                CodigoTrabajador = m.Empleado.CodigoTrabajador,
                FechaHoraServidor = m.FechaHoraServidor,
                FechaHoraDispositivo = m.FechaHoraDispositivo,
                TipoMovimiento = m.TipoMovimiento,
                Origen = m.Origen,
                MinutosAtraso = m.MinutosAtraso,
                FueraDeGeocerca = m.FueraDeGeocerca,
                Latitud = m.Latitud,
                Longitud = m.Longitud
            })
            .ToListAsync();

        return Ok(reporte);
    }

    // GET: api/Reportes/resumen-empleados?fechaInicio=2026-10-01&fechaFin=2026-10-31
    [HttpGet("resumen-empleados")]
    public async Task<IActionResult> ObtenerResumenPorEmpleado([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var inicioUtc = DateTime.SpecifyKind(fechaInicio.Date, DateTimeKind.Utc);
        var finUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var empleados = await _context.Empleados
            .Where(e => e.EmpresaId == empresaId && e.Activo)
            .ToListAsync();

        var empleadosIds = empleados.Select(e => e.Id).ToList();

        var marcaciones = await _context.Marcaciones
            .Where(m => empleadosIds.Contains(m.EmpleadoId) 
                     && m.FechaHoraServidor >= inicioUtc 
                     && m.FechaHoraServidor <= finUtc)
            .ToListAsync();

        var resumen = empleados.Select(emp =>
        {
            var marcacionesEmp = marcaciones.Where(m => m.EmpleadoId == emp.Id).ToList();
            var entradas = marcacionesEmp.Where(m => m.TipoMovimiento.ToUpper() == "ENTRADA").ToList();

            return new
            {
                EmpleadoId = emp.Id,
                NombreCompleto = emp.NombreCompleto,
                CodigoTrabajador = emp.CodigoTrabajador,
                TotalDiasAsistidos = entradas.Select(m => m.FechaHoraServidor.Date).Distinct().Count(),
                TotalMinutosAtraso = entradas.Sum(m => m.MinutosAtraso),
                TotalAlertasGeocerca = marcacionesEmp.Count(m => m.FueraDeGeocerca),
                TotalMarcaciones = marcacionesEmp.Count
            };
        });

        return Ok(resumen);
    }

    // GET: api/Reportes/exportar-csv?fechaInicio=2026-10-01&fechaFin=2026-10-31
    [HttpGet("exportar-csv")]
    public async Task<IActionResult> ExportarCsv([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var inicioUtc = DateTime.SpecifyKind(fechaInicio.Date, DateTimeKind.Utc);
        var finUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var marcaciones = await _context.Marcaciones
            .Include(m => m.Empleado)
            .Where(m => m.Empleado != null 
                     && m.Empleado.EmpresaId == empresaId 
                     && m.FechaHoraServidor >= inicioUtc 
                     && m.FechaHoraServidor <= finUtc)
            .OrderBy(m => m.FechaHoraServidor)
            .ToListAsync();

        var builder = new StringBuilder();
        
        // Encabezados con separador punto y coma ';' compatible con regiones de habla hispana en Excel
        builder.AppendLine("ID Marcacion;Empleado ID;Nombre Empleado;Codigo Trabajador;Fecha Hora Servidor (UTC);Tipo Movimiento;Origen;Minutos Atraso;Fuera de Geocerca");

        foreach (var m in marcaciones)
        {
            var linea = $"{m.Id};{m.EmpleadoId};\"{m.Empleado?.NombreCompleto}\";\"{m.Empleado?.CodigoTrabajador}\";{m.FechaHoraServidor:yyyy-MM-dd HH:mm:ss};{m.TipoMovimiento};{m.Origen};{m.MinutosAtraso};{(m.FueraDeGeocerca ? "SI" : "NO")}";
            builder.AppendLine(linea);
        }

        // Registro de Auditoría
        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "EXPORTAR_REPORTE_MARCACIONES_CSV",
            Detalle = $"Descarga de marcaciones desde {fechaInicio:yyyy-MM-dd} hasta {fechaFin:yyyy-MM-dd}. Total filas: {marcaciones.Count}",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });
        await _context.SaveChangesAsync();

        // BOM UTF-8 para garantizar codificación correcta en Excel
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        
        var nombreArchivo = $"Reporte_Asistencia_{fechaInicio:yyyyMMdd}_a_{fechaFin:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
    }

    // GET: api/Reportes/exportar-resumen-csv?fechaInicio=2026-10-01&fechaFin=2026-10-31
    [HttpGet("exportar-resumen-csv")]
    public async Task<IActionResult> ExportarResumenCsv([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFin)
    {
        int empresaId = ObtenerEmpresaIdDelToken();
        if (empresaId == 0) return Unauthorized("Token inválido.");

        var inicioUtc = DateTime.SpecifyKind(fechaInicio.Date, DateTimeKind.Utc);
        var finUtc = DateTime.SpecifyKind(fechaFin.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var empleados = await _context.Empleados
            .Where(e => e.EmpresaId == empresaId && e.Activo)
            .ToListAsync();

        var empleadosIds = empleados.Select(e => e.Id).ToList();

        var marcaciones = await _context.Marcaciones
            .Where(m => empleadosIds.Contains(m.EmpleadoId) 
                     && m.FechaHoraServidor >= inicioUtc 
                     && m.FechaHoraServidor <= finUtc)
            .ToListAsync();

        var builder = new StringBuilder();
        builder.AppendLine("Empleado ID;Codigo Trabajador;Nombre Completo;Total Dias Asistidos;Total Minutos Atraso;Total Alertas Geocerca;Total Marcaciones");

        foreach (var emp in empleados)
        {
            var marcacionesEmp = marcaciones.Where(m => m.EmpleadoId == emp.Id).ToList();
            var entradas = marcacionesEmp.Where(m => m.TipoMovimiento.ToUpper() == "ENTRADA").ToList();

            int diasAsistidos = entradas.Select(m => m.FechaHoraServidor.Date).Distinct().Count();
            int minutosAtraso = entradas.Sum(m => m.MinutosAtraso);
            int alertasGeocerca = marcacionesEmp.Count(m => m.FueraDeGeocerca);
            int totalMarcaciones = marcacionesEmp.Count;

            var linea = $"{emp.Id};\"{emp.CodigoTrabajador}\";\"{emp.NombreCompleto}\";{diasAsistidos};{minutosAtraso};{alertasGeocerca};{totalMarcaciones}";
            builder.AppendLine(linea);
        }

        // Registro de Auditoría
        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = empresaId,
            Accion = "EXPORTAR_REPORTE_RESUMEN_CSV",
            Detalle = $"Descarga de reporte resumido por empleado desde {fechaInicio:yyyy-MM-dd} hasta {fechaFin:yyyy-MM-dd}. Empleados procesados: {empleados.Count}",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = ObtenerUsuarioDelToken()
        });
        await _context.SaveChangesAsync();

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        var nombreArchivo = $"Reporte_Resumen_Empleados_{fechaInicio:yyyyMMdd}_a_{fechaFin:yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
    }
}