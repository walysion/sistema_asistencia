namespace AsistenciaCore.Api.Models;

public class AuditoriaLog
{
    public long Id { get; set; }
    public int EmpresaId { get; set; }
    public string Accion { get; set; } = string.Empty; // Ej: "CIERRE_DIARIO_INASISTENCIAS", "INICIO_SESION"
    public string Detalle { get; set; } = string.Empty;
    public DateTime FechaHoraUtc { get; set; } = DateTime.UtcNow;
    public string? Usuario { get; set; }
}