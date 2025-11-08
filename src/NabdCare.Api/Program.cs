using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Configurations;
using NabdCare.Api.Extensions;
using NabdCare.Api.Middleware;
using NabdCare.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using DotNetEnv;
using NabdCare.Application.Common.Constants;
using NabdCare.Application.DTOs.Common;
using NabdCare.Infrastructure.Persistence.DataSeed;

// ✅ Disable legacy JWT claim mapping
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Bind configuration objects
builder.Services.Configure<FrontendSettings>(builder.Configuration);

// Add services (DB, auth, repos, application services)
builder.Services.AddNabdCareServices(builder.Configuration);

// Controllers + enum serialization as strings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ✅ Handle FluentValidation model errors consistently
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => e.Key,
                e => e.Value!.Errors
                    .Select(x => x.ErrorMessage)
                    .ToArray()
            );

        var traceId = Guid.NewGuid().ToString("N");
        context.HttpContext.Response.Headers["X-Trace-Id"] = traceId;

        var isDevelopment = context.HttpContext.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsDevelopment();

        var errorResponse = new ApiErrorResponse
        {
            Error = new ErrorResponseDto
            {
                Message = "Validation failed. Please check your input.",
                Code = ErrorCodes.VALIDATION_ERROR,
                Type = "BadRequest",
                StatusCode = StatusCodes.Status400BadRequest,
                TraceId = traceId,
                Details = isDevelopment ? new { validationErrors = errors } : null
            }
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = isDevelopment
        };

        return new BadRequestObjectResult(errorResponse);
    };
});

// ✅ Auth + RBAC
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);

// ✅ Default: authenticated users required unless [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ✅ Dynamic RBAC policy provider + handler
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ✅ Swagger
SwaggerConfig.AddSwagger(builder.Services);

// ✅ CORS configuration
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ✅ Seed DB only outside Test environment
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddHostedService<DbSeedHostedService>();
}

var app = builder.Build();

// ✅ Global exception handling FIRST
app.UseMiddleware<ErrorHandlingMiddleware>();

// ✅ Allow Swagger UI always
app.UseSwagger();
app.UseSwaggerUI(opts =>
{
    opts.SwaggerEndpoint("/swagger/v1/swagger.json", "NabdCare API v1");
    opts.RoutePrefix = "swagger";
});

// ✅ Reverse Proxy (Nginx/Traefik/Kubernetes)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ✅ Enforce HTTPS only in Prod
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// ✅ CORS + Security Headers (CSP, XSS etc)
app.UseCors("AllowFrontend");
app.UseSecurityHeaders();

// ✅ Authentication must run before authorization
app.UseAuthentication();
app.UseAuthorization();

// ✅ Audit logs AFTER auth to attach user identity
app.UseMiddleware<AuditLoggingMiddleware>();

// ✅ Subscription restrictions AFTER we know the user & tenant
app.UseMiddleware<SubscriptionValidationMiddleware>();

// ✅ Rate limiting after auth (not for Test env)
if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

// ✅ ✅ All endpoints mapped here
app.MapGroup("/api")
   .MapAllEndpoints();


app.Run();

public partial class Program {}