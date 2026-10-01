using Microsoft.EntityFrameworkCore;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) 
    { 
    }

    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Empleado> Empleados { get; set; }
    public DbSet<Marcacion> Marcaciones { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Turno> Turnos { get; set; }
    public DbSet<Geocerca> Geocercas { get; set; }
    public DbSet<SolicitudPermiso> SolicitudesPermiso { get; set; }
    public DbSet<AuditoriaLog> AuditoriaLogs { get; set; }
    public DbSet<TerminalKiosco> TerminalesKiosco { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Unicidad del Código de Trabajador POR Empresa (Multitenant Isolation)
        modelBuilder.Entity<Empleado>()
            .HasIndex(e => new { e.EmpresaId, e.CodigoTrabajador })
            .IsUnique();

        // 2. Unicidad del Correo Electrónico de Usuario
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // 3. Unicidad de ApiKey para Terminales Kiosco
        modelBuilder.Entity<TerminalKiosco>()
            .HasIndex(t => t.ApiKey)
            .IsUnique();

        // 4. Índices para acelerar búsquedas de Marcaciones por Fecha y Empleado
        modelBuilder.Entity<Marcacion>()
            .HasIndex(m => new { m.EmpleadoId, m.FechaHoraServidor });
    }
}