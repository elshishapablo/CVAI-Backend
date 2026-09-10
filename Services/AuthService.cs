using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CVMatchAI.API.Data;
using CVMatchAI.API.DTOs;
using CVMatchAI.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CVMatchAI.API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        // Verificar email duplicado
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
            throw new InvalidOperationException("Ya existe una cuenta con ese email.");

        var user = new User
        {
            Name          = dto.Name.Trim(),
            Email         = dto.Email.ToLower().Trim(),
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Plan          = "free",
            LastResetDate = DateTime.UtcNow,
            CreatedAt     = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower())
            ?? throw new UnauthorizedAccessException("Credenciales incorrectas.");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales incorrectas.");

        // Resetear contador si cambió el mes
        ResetMonthlyCountIfNeeded(user);
        await _db.SaveChangesAsync();

        return BuildAuthResponse(user);
    }

    public async Task<UserDto> GetUserByIdAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        ResetMonthlyCountIfNeeded(user);
        await _db.SaveChangesAsync();

        return MapToUserDto(user);
    }

    public string GenerateJwt(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim("plan", user.Plan)
        };

        if (!int.TryParse(_config["JwtSettings:ExpirationDays"], out var days) || days <= 0)
            days = 7;

        var token = new JwtSecurityToken(
            issuer:            _config["JwtSettings:Issuer"] ?? "CVMatchAPI",
            audience:          _config["JwtSettings:Audience"] ?? "CVMatchClient",
            claims:            claims,
            expires:           DateTime.UtcNow.AddDays(days),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ──────────────────── helpers privados ────────────────────

    private AuthResponseDto BuildAuthResponse(User user) => new()
    {
        Token = GenerateJwt(user),
        User  = MapToUserDto(user)
    };

    private static UserDto MapToUserDto(User user) => new()
    {
        Id                    = user.Id,
        Name                  = user.Name,
        Email                 = user.Email,
        Plan                  = user.Plan,
        AnalysisUsedThisMonth = user.AnalysisUsedThisMonth,
        AnalysisLimit         = user.Plan == "pro" ? -1 : 3,
        CreatedAt             = user.CreatedAt
    };

    /// <summary>Resetea el contador si estamos en un mes distinto al del último reset</summary>
    public static void ResetMonthlyCountIfNeeded(User user)
    {
        var now = DateTime.UtcNow;
        if (now.Year != user.LastResetDate.Year || now.Month != user.LastResetDate.Month)
        {
            user.AnalysisUsedThisMonth = 0;
            user.LastResetDate         = now;
        }
    }
}
