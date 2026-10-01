using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class Empleado
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoTrabajador { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }

    // Relación opcional con el Turno de trabajo
    public int? TurnoId { get; set; }

    [JsonIgnore]
    public Turno? Turno { get; set; }
}