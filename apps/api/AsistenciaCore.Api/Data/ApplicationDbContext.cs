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
}