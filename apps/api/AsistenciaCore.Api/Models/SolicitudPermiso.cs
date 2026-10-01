using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class SolicitudPermiso
{
    public int Id { get; set; }
    public int EmpleadoId { get; set; }

    [JsonIgnore]
    public Empleado? Empleado { get; set; }

    public string TipoPermiso { get; set; } = "LICENCIA_MEDICA"; // LICENCIA_MEDICA, VACACIONES, PERMISO_ADMINISTRATIVO, JUSTIFICACION_ATRASO
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public string Estado { get; set; } = "PENDIENTE"; // PENDIENTE, APROBADO, RECHAZADO
    public DateTime FechaSolicitud { get; set; } = DateTime.UtcNow;

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }
}