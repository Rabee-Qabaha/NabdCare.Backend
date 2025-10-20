using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
        
        // ✅ P0 FIX: Validate JWT configuration on startup
        ValidateConfiguration();
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
            new Claim("fullName", fullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // ✅ Unique token ID
        };

        // ✅ P0 FIX: Only add ClinicId if it has a value (cleaner JWT)
        if (clinicId.HasValue)
        {
            claims.Add(new Claim("ClinicId", clinicId.Value.ToString()));
        }

        // Signing key
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Validates JWT configuration on application startup
    /// </summary>
    private void ValidateConfiguration()
    {
        var key = Environment.GetEnvironmentVariable("JWT_KEY")
                  ?? _config["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogCritical("❌ JWT Key is not configured. Set JWT_KEY environment variable or Jwt:Key in appsettings.json");
            throw new InvalidOperationException("JWT Key not configured");
        }

        // Validate key strength
        if (key.Length < 32)
        {
            _logger.LogCritical("❌ JWT Key is too short ({Length} chars). Must be at least 32 characters (256 bits)", key.Length);
            throw new InvalidOperationException($"JWT Key must be at least 32 characters. Current length: {key.Length}");
        }

        // ✅ Warn if using weak/example keys
        var weakKeyPatterns = new[] { "your-secret", "example", "test-key", "change-me", "secret", "password" };
        if (weakKeyPatterns.Any(pattern => key.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogCritical("❌ JWT Key appears to contain weak/example patterns. Use a strong random key.");
            throw new InvalidOperationException("JWT Key appears to be weak or an example key");
        }

        // ✅ Validate other required settings
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? _config["Jwt:Issuer"];
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? _config["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            _logger.LogCritical("❌ JWT Issuer is not configured");
            throw new InvalidOperationException("JWT Issuer not configured");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            _logger.LogCritical("❌ JWT Audience is not configured");
            throw new InvalidOperationException("JWT Audience not configured");
        }

        _logger.LogInformation("✅ JWT configuration validated successfully");
        _logger.LogInformation("   - Key length: {KeyLength} characters", key.Length);
        _logger.LogInformation("   - Issuer: {Issuer}", issuer);
        _logger.LogInformation("   - Audience: {Audience}", audience);
    }
}