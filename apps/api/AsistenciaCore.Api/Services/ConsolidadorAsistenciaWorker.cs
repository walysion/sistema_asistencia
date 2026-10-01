using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Services;

public class ConsolidadorAsistenciaWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConsolidadorAsistenciaWorker> _logger;

    public ConsolidadorAsistenciaWorker(IServiceProvider serviceProvider, ILogger<ConsolidadorAsistenciaWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Servicio de Consolidación Automática de Asistencia iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarInasistenciasDelDiaAnterior();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la consolidación automática en segundo plano.");
            }

            // Ejecuta el ciclo de verificación cada 1 hora
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task ProcesarInasistenciasDelDiaAnterior()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var ayerUtc = DateTime.UtcNow.Date.AddDays(-1);
        var inicioAyerUtc = DateTime.SpecifyKind(ayerUtc, DateTimeKind.Utc);
        var finAyerUtc = DateTime.SpecifyKind(ayerUtc.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

        var empresas = await context.Empresas.ToListAsync();

        foreach (var empresa in empresas)
        {
            var empleadosActivos = await context.Empleados
                .Where(e => e.EmpresaId == empresa.Id && e.Activo)
                .ToListAsync();

            var idsEmpleadosConMarcacion = await context.Marcaciones
                .Where(m => m.Empleado != null 
                         && m.Empleado.EmpresaId == empresa.Id 
                         && m.FechaHoraServidor >= inicioAyerUtc 
                         && m.FechaHoraServidor <= finAyerUtc 
                         && m.TipoMovimiento.ToUpper() == "ENTRADA")
                .Select(m => m.EmpleadoId)
                .Distinct()
                .ToListAsync();

            var idsEmpleadosConPermiso = await context.SolicitudesPermiso
                .Where(p => p.EmpresaId == empresa.Id 
                         && p.Estado.ToUpper() == "APROBADO" 
                         && p.FechaInicio <= finAyerUtc 
                         && p.FechaFin >= inicioAyerUtc)
                .Select(p => p.EmpleadoId)
                .Distinct()
                .ToListAsync();

            int inasistenciasDetectadas = 0;

            foreach (var emp in empleadosActivos)
            {
                bool marcoPresente = idsEmpleadosConMarcacion.Contains(emp.Id);
                bool tienePermiso = idsEmpleadosConPermiso.Contains(emp.Id);

                if (!marcoPresente && !tienePermiso)
                {
                    inasistenciasDetectadas++;
                }
            }

            if (inasistenciasDetectadas > 0)
            {
                context.AuditoriaLogs.Add(new AuditoriaLog
                {
                    EmpresaId = empresa.Id,
                    Accion = "CONSOLIDACION_INASISTENCIAS_DIARIAS",
                    Detalle = $"Fecha evaluada: {ayerUtc:yyyy-MM-dd}. Empleados inasistentes detectados: {inasistenciasDetectadas}",
                    FechaHoraUtc = DateTime.UtcNow,
                    Usuario = "SYSTEM_WORKER"
                });

                await context.SaveChangesAsync();
            }
        }
    }
}