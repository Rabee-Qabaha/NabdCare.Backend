using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NabdCare.Application.Interfaces;

namespace NabdCare.Infrastructure.Repositories.Auth;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration config, ILogger<JwtTokenService> logger)
    {
        _config = config;
        _logger = logger;
        ValidateConfiguration();
    }

    public string GenerateToken(string userId, string email, string roleName, Guid roleId, Guid? clinicId, string fullName)
    {
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
        _ = double.TryParse(expireMinutesStr, out var expireMinutes);
        if (expireMinutes <= 0) expireMinutes = 60;

        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),                       
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("email", email),
            new("name", fullName),
            new("role", roleName),
            new("roleId", roleId.ToString())
        };
        
        if (clinicId.HasValue)
            claims.Add(new Claim("ClinicId", clinicId.Value.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        Span<byte> randomBytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(randomBytes);

        // ✅ URL-safe Base64 for cookie/query compatibility
        return WebEncoders.Base64UrlEncode(randomBytes);
    }

    private void ValidateConfiguration()
    {
        var key = Environment.GetEnvironmentVariable("JWT_KEY") ?? _config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("JWT Key not configured");
        if (key.Length < 32) throw new InvalidOperationException("JWT Key must be at least 32 characters (256-bit)");

        var weakPatterns = new[] { "secret", "password", "example", "test-key", "change-me", "your-secret" };
        if (weakPatterns.Any(w => key.Contains(w, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Weak JWT key detected");

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _config["Jwt:Issuer"]))
            throw new InvalidOperationException("JWT Issuer not configured");

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _config["Jwt:Audience"]))
            throw new InvalidOperationException("JWT Audience not configured");

        _logger.LogInformation("✅ JWT configuration validated.");
    }
}
