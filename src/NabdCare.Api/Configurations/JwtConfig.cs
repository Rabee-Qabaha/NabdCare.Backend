using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace NabdCare.Api.Configurations;

public static class JwtConfig
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // ✅ Read from environment first, fallback to appsettings.json
        var key = Environment.GetEnvironmentVariable("JWT_KEY") 
                  ?? configuration["Jwt:Key"];
        
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                     ?? configuration["Jwt:Issuer"];
        
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                       ?? configuration["Jwt:Audience"];

        // ✅ P0 FIX: Validate configuration
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JWT Key is not configured. Set JWT_KEY environment variable or Jwt:Key in appsettings.json.");

        if (key.Length < 32)
            throw new InvalidOperationException($"JWT Key is too short ({key.Length} chars). Must be at least 32 characters (256 bits).");

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
                IssuerSigningKey = signingKey,

                ClockSkew = TimeSpan.FromMinutes(5),

                // ✅ REQUIRED when DefaultMapInboundClaims = false
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    
                    // ✅ P0 FIX: Add token expiry header
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("X-Token-Expired", "true");
                        logger.LogWarning("⚠️ JWT expired for user");
                    }
                    else
                    {
                        logger.LogError(context.Exception, "❌ JWT authentication failed");
                    }

                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();

                    var userId = context.Principal?.FindFirst("sub")?.Value ?? "unknown";
                    var email = context.Principal?.FindFirst("email")?.Value ?? "unknown";

                    logger.LogInformation("✅ JWT validated for user {UserId} ({Email})", userId, email);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    // ✅ P0 FIX: Provide consistent error response
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    
                    var traceId = Guid.NewGuid().ToString("N");
                    context.Response.Headers.Append("X-Trace-Id", traceId);
                    
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    
                    logger.LogWarning("⚠️ JWT challenge triggered. Error={Error}, Description={ErrorDescription}, TraceId={TraceId}",
                        context.Error, context.ErrorDescription, traceId);
                    
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = new
                        {
                            message = "Authentication failed. Please login again.",
                            type = "Unauthorized",
                            statusCode = 401,
                            traceId
                        }
                    });
                    
                    return context.Response.WriteAsync(result);
                }
            };
        });

        return services;
    }
}