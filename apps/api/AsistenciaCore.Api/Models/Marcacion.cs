using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class Marcacion
{
    public long Id { get; set; }
    public int EmpleadoId { get; set; }

    [JsonIgnore]
    public Empleado? Empleado { get; set; }

    public DateTime FechaHoraServidor { get; set; } = DateTime.UtcNow;
    public DateTime FechaHoraDispositivo { get; set; }

    public string TipoMovimiento { get; set; } = "ENTRADA";
    public string Origen { get; set; } = "APP_MOVIL";

    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public bool FueraDeGeocerca { get; set; } = false;

    public bool EsSincronizacionOffline { get; set; } = false;
    public string? DispositivoId { get; set; }

    // Minutos de atraso calculados automáticamente por el sistema
    public int MinutosAtraso { get; set; } = 0;
}