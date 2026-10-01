using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // 1. Aplicar automáticamente cualquier migración de EF Core pendiente
        await context.Database.MigrateAsync();

        // 2. Sembrar datos iniciales de prueba si la base de datos está vacía
        if (!await context.Empresas.AnyAsync())
        {
            var empresaDemo = new Empresa
            {
                RazonSocial = "Empresa Demo SaaS",
                DocumentoIdentidad = "76123456-7",
                FechaCreacion = DateTime.UtcNow
            };

            context.Empresas.Add(empresaDemo);
            await context.SaveChangesAsync();

            // Turno por defecto
            var turnoDemo = new Turno
            {
                Nombre = "Jornada Ordinaria",
                HoraEntrada = new TimeSpan(9, 0, 0),
                HoraSalida = new TimeSpan(18, 0, 0),
                ToleranciaMinutos = 15,
                MinutosColacion = 60,
                EmpresaId = empresaDemo.Id
            };
            context.Turnos.Add(turnoDemo);

            // Usuario Administrador por defecto
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            var usuarioAdmin = new Usuario
            {
                Email = "admin@empresademo.com",
                PasswordHash = passwordHash,
                Rol = "ADMIN_EMPRESA",
                EmpresaId = empresaDemo.Id
            };
            context.Usuarios.Add(usuarioAdmin);

            await context.SaveChangesAsync();
        }
    }
}