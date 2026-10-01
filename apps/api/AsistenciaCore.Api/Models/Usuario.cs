using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "ADMIN_EMPRESA"; // Ej: "ADMIN_EMPRESA", "SUPER_ADMIN", "RECURSOS_HUMANOS"
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Campos para la gestión segura de Refresh Tokens
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }
}