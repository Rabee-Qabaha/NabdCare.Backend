using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NabdCare.Application.Interfaces;

namespace NabdCare.Infrastructure.Repositories.Auth;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Generates a signed JWT access token with clean, frontend-friendly claims.
    /// </summary>
    public string GenerateToken(string userId, string email, string role, Guid? clinicId, string fullName)
    {
        // Prefer env vars (for Docker/prod), fallback to appsettings.json
        var key = Environment.GetEnvironmentVariable("JWT_KEY")
                  ?? _config["Jwt:Key"]
                  ?? throw new InvalidOperationException("JWT Key not configured");

        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
                     ?? _config["Jwt:Issuer"]
                     ?? throw new InvalidOperationException("JWT Issuer not configured");

        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                       ?? _config["Jwt:Audience"]
                       ?? throw new InvalidOperationException("JWT Audience not configured");

        var expireMinutesStr = Environment.GetEnvironmentVariable("JWT_EXPIREMINUTES")
                               ?? _config["Jwt:ExpireMinutes"]
                               ?? "60";

        if (!double.TryParse(expireMinutesStr, out var expireMinutes))
            expireMinutes = 60;

        // ✅ Clean claim names for frontend simplicity
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("role", role),
            new Claim("ClinicId", clinicId?.ToString() ?? string.Empty),
            new Claim("fullName", fullName)
        };

        // 🧩 (Optional backward compatibility: keep the old ClaimTypes.Role if you wish)
        // claims.Add(new Claim(ClaimTypes.Role, role));

        // Signing key
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}