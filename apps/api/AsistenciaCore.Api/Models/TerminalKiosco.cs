using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class TerminalKiosco
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime UltimaConexionUtc { get; set; } = DateTime.UtcNow;

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }

    public int? GeocercaId { get; set; }

    [JsonIgnore]
    public Geocerca? Geocerca { get; set; }
}