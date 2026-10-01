using System.Text.Json.Serialization;

namespace AsistenciaCore.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "ADMIN_EMPRESA";

    public int EmpresaId { get; set; }

    [JsonIgnore]
    public Empresa? Empresa { get; set; }
}