using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace NabdCare.Api.Configurations;

public static class JwtConfig
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Read from environment first, fallback to appsettings.json
        var key = Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"];
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? configuration["Jwt:Issuer"];
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? configuration["Jwt:Audience"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT Key is not configured. Set JWT_KEY environment variable or Jwt:Key in appsettings.json.");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JWT Issuer is not configured. Set JWT_ISSUER environment variable or Jwt:Issuer in appsettings.json.");

        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT Audience is not configured. Set JWT_AUDIENCE environment variable or Jwt:Audience in appsettings.json.");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = signingKey
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogError(context.Exception, "❌ JWT authentication failed");

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();

                    var claims = string.Join(", ",
                        context.Principal.Claims.Select(c => $"{c.Type}={c.Value}"));

                    logger.LogInformation("✅ JWT validated. Claims: {Claims}", claims);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("⚠️ JWT challenge triggered. Error={Error}, Description={ErrorDescription}",
                        context.Error, context.ErrorDescription);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}