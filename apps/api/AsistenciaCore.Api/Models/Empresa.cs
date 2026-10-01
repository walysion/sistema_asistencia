using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class Empresa
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string DocumentoIdentidad { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}