using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Models;

namespace AsistenciaCore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var existeUsuario = await _context.Usuarios.AnyAsync(u => u.Email == dto.Email);
        if (existeUsuario)
        {
            return BadRequest("El email ya está registrado.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var usuario = new Usuario
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            EmpresaId = dto.EmpresaId,
            Rol = string.IsNullOrWhiteSpace(dto.Rol) ? "ADMIN_EMPRESA" : dto.Rol
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok(new { mensaje = "Usuario registrado correctamente", usuarioId = usuario.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return Unauthorized("Credenciales inválidas.");
        }

        var token = GenerarJwtToken(usuario);
        return Ok(new { token });
    }

    private string GenerarJwtToken(Usuario usuario)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "ClaveSuperSecretaSaaSAsistencia2026_UltraSecureKey!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim("EmpresaId", usuario.EmpresaId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "AsistenciaCore",
            audience: _configuration["Jwt:Audience"] ?? "AsistenciaClients",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record RegisterDto(string Email, string Password, int EmpresaId, string? Rol);
public record LoginDto(string Email, string Password);