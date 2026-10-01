using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class Turno
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty; // Ej: "Turno Administrativo", "Turno Nocturno"
    public TimeSpan HoraEntrada { get; set; }         // Ej: 09:00:00
    public TimeSpan HoraSalida { get; set; }          // Ej: 18:00:00
    public int ToleranciaMinutos { get; set; } = 15;  // Minutos de gracia antes de marcar atraso
    public int MinutosColacion { get; set; } = 60;    // Tiempo de almuerzo/colación

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }
}