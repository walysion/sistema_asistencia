using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class Geocerca
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; // Ej: "Oficina Central", "Planta Industrial"
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public double RadioMetros { get; set; } = 100; // Radio permitido en metros
    public bool Activa { get; set; } = true;

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }
}