using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class TerminalKiosco
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; // Ej: "Tablet Recepción Piso 3", "Kiosco Entradas Faena Norte"
    public string NumeroSerie { get; set; } = string.Empty; // Identificador único del hardware
    public string ApiKey { get; set; } = string.Empty; // Clave secreta para autenticación del terminal
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public DateTime? UltimaConexion { get; set; }

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }

    public int? GeocercaId { get; set; }

    [JsonIgnore]
    public Geocerca? Geocerca { get; set; }
}