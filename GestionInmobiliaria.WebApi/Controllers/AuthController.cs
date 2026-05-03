using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GestionInmobiliaria.Dominio.Entidades;
using GestionInmobiliaria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GestionInmobiliaria.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _context;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            Nombre = request.Nombre,
            Apellido = request.Apellido
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var rol = string.IsNullOrWhiteSpace(request.Rol) ? "Operador" : request.Rol;
        if (!await _roleManager.RoleExistsAsync(rol))
            await _roleManager.CreateAsync(new IdentityRole(rol));
        await _userManager.AddToRoleAsync(user, rol);

        return Ok(new { message = "Usuario creado correctamente." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.Activo)
            return Unauthorized(new { message = "Credenciales inválidas." });

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { message = "Credenciales inválidas." });

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = GenerarAccessToken(user, roles);
        var refreshToken = await GenerarYGuardarRefreshTokenAsync(user.Id);

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email!,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Roles = roles.ToList()
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var rt = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        if (rt is null || !rt.EsValido)
            return Unauthorized(new { message = "Refresh token inválido o expirado." });

        var user = await _userManager.FindByIdAsync(rt.UserId);
        if (user is null || !user.Activo)
            return Unauthorized(new { message = "Usuario no encontrado o inactivo." });

        rt.FechaRevocacion = DateTime.UtcNow;
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = GenerarAccessToken(user, roles);
        var newRefreshToken = await GenerarYGuardarRefreshTokenAsync(user.Id);

        await _context.SaveChangesAsync();

        return Ok(new TokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            Email = user.Email!,
            Nombre = user.Nombre,
            Apellido = user.Apellido,
            Roles = roles.ToList()
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        var rt = await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken);

        if (rt is not null)
        {
            rt.FechaRevocacion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Sesión cerrada correctamente." });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user is null)
            return NotFound(new { message = "Usuario no encontrado." });

        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Apellido))
            return BadRequest(new { message = "Nombre y apellido son requeridos." });

        user.Nombre = request.Nombre;
        user.Apellido = request.Apellido;
        await _userManager.UpdateAsync(user);

        return Ok(new { message = "Perfil actualizado correctamente." });
    }

    [HttpPatch("usuarios/{id}/activo")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CambiarActivo(string id, [FromBody] CambiarActivoRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound(new { message = "Usuario no encontrado." });

        user.Activo = request.Activo;
        await _userManager.UpdateAsync(user);

        var estado = request.Activo ? "activado" : "desactivado";
        return Ok(new { message = $"Usuario {estado} correctamente." });
    }

    private string GenerarAccessToken(ApplicationUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, $"{user.Nombre} {user.Apellido}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpirationMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> GenerarYGuardarRefreshTokenAsync(string userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var days = int.Parse(_config["Jwt:RefreshTokenDays"]!);

        _context.RefreshTokens.Add(new RefreshToken
        {
            Token = token,
            UserId = userId,
            Expiracion = DateTime.UtcNow.AddDays(days),
            FechaCreacion = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return token;
    }
}

public record RegisterRequest(string Email, string Password, string Nombre, string Apellido, string? Rol);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record UpdateProfileRequest(string Nombre, string Apellido);
public record CambiarActivoRequest(bool Activo);
public record TokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Apellido { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
}
