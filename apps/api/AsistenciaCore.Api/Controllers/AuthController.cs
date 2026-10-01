using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

    // POST: api/Auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUsuarioDto dto)
    {
        var empresaExiste = await _context.Empresas.AnyAsync(e => e.Id == dto.EmpresaId);
        if (!empresaExiste)
        {
            return BadRequest("La empresa especificada no existe.");
        }

        var usuarioExistente = await _context.Usuarios.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (usuarioExistente)
        {
            return BadRequest("El correo electrónico ya se encuentra registrado.");
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var usuario = new Usuario
        {
            Email = dto.Email.ToLower(),
            PasswordHash = passwordHash,
            Rol = string.IsNullOrWhiteSpace(dto.Rol) ? "ADMIN_EMPRESA" : dto.Rol.ToUpper(),
            EmpresaId = dto.EmpresaId,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Usuario registrado con éxito.", UsuarioId = usuario.Id, Email = usuario.Email });
    }

    // POST: api/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUsuarioDto dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return Unauthorized("Credenciales inválidas.");
        }

        string accessToken = GenerarAccessToken(usuario);
        string refreshToken = GenerarRefreshToken();

        usuario.RefreshToken = refreshToken;
        usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Validez de 7 días

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = usuario.EmpresaId,
            Accion = "INICIO_SESION",
            Detalle = $"Inicio de sesión exitoso para el usuario {usuario.Email}",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = usuario.Email
        });

        await _context.SaveChangesAsync();

        return Ok(new AuthResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiracionUtc: DateTime.UtcNow.AddMinutes(60),
            Email: usuario.Email,
            Rol: usuario.Rol
        ));
    }

    // POST: api/Auth/refresh-token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AccessToken) || string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            return BadRequest("Solicitud de renovación inválida.");
        }

        var principal = ObtenerPrincipalDeTokenExpirado(dto.AccessToken);
        if (principal == null)
        {
            return BadRequest("Token de acceso o clave de firma inválidos.");
        }

        var usuarioEmail = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("sub")?.Value;
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == usuarioEmail);

        if (usuario == null || usuario.RefreshToken != dto.RefreshToken || usuario.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized("Refresh token expirado, inválido o sesión revocada.");
        }

        string nuevoAccessToken = GenerarAccessToken(usuario);
        string nuevoRefreshToken = GenerarRefreshToken();

        usuario.RefreshToken = nuevoRefreshToken;
        usuario.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _context.SaveChangesAsync();

        return Ok(new AuthResponseDto(
            AccessToken: nuevoAccessToken,
            RefreshToken: nuevoRefreshToken,
            ExpiracionUtc: DateTime.UtcNow.AddMinutes(60),
            Email: usuario.Email,
            Rol: usuario.Rol
        ));
    }

    // POST: api/Auth/revoke (Cierre de sesión seguro en todos los dispositivos)
    [Authorize]
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("sub")?.Value;
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        if (usuario == null) return NotFound("Usuario no encontrado.");

        usuario.RefreshToken = null;
        usuario.RefreshTokenExpiryTime = null;

        _context.AuditoriaLogs.Add(new AuditoriaLog
        {
            EmpresaId = usuario.EmpresaId,
            Accion = "CIERRE_SESION_REVOCACION",
            Detalle = $"Tokens de acceso revocados para {usuario.Email}",
            FechaHoraUtc = DateTime.UtcNow,
            Usuario = usuario.Email
        });

        await _context.SaveChangesAsync();

        return Ok(new { Mensaje = "Sesión cerrada y Refresh Token revocado exitosamente." });
    }

    private string GenerarAccessToken(Usuario usuario)
    {
        var secretKey = _configuration["Jwt:Secret"] ?? "ClaveSuperSecretaSaaSAsistencia2026_UltraSecureKey!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Email),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("UsuarioId", usuario.Id.ToString()),
            new Claim("EmpresaId", usuario.EmpresaId.ToString()),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "AsistenciaCore",
            audience: _configuration["Jwt:Audience"] ?? "AsistenciaClients",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerarRefreshToken()
    {
        var numeroAleatorio = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(numeroAleatorio);
        return Convert.ToBase64String(numeroAleatorio);
    }

    private ClaimsPrincipal? ObtenerPrincipalDeTokenExpirado(string token)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "ClaveSuperSecretaSaaSAsistencia2026_UltraSecureKey!";
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"] ?? "AsistenciaClients",
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"] ?? "AsistenciaCore",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = false // Importante: ignorar expiración del AccessToken para permitir la renovación
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }
}

public record RegisterUsuarioDto(string Email, string Password, int EmpresaId, string? Rol);
public record LoginUsuarioDto(string Email, string Password);
public record RefreshTokenRequestDto(string AccessToken, string RefreshToken);
public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiracionUtc, string Email, string Rol);