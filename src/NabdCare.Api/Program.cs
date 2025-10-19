using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Configurations;
using NabdCare.Api.Extensions;
using NabdCare.Api.Middleware;
using NabdCare.Infrastructure.Persistence.DataSeed;

// Load .env file at the very start
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Bind configurations
// --------------------
builder.Services.Configure<FrontendSettings>(builder.Configuration);

// --------------------
// Add services
// --------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// ✅ P0 FIX: Configure automatic model validation responses
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .SelectMany(e => e.Value!.Errors.Select(x => x.ErrorMessage))
            .ToList();

        var traceId = Guid.NewGuid().ToString("N");
        context.HttpContext.Response.Headers["X-Trace-Id"] = traceId;

        return new BadRequestObjectResult(new
        {
            error = new
            {
                message = "Validation failed",
                type = "ValidationError",
                statusCode = 400,
                traceId,
                details = errors
            }
        });
    };
});

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddNabdCareServices(builder.Configuration);

// ✅ P0 FIX: Add Rate Limiting (extracted to extension method)
builder.Services.AddRateLimiting(builder.Configuration);

// Swagger
SwaggerConfig.AddSwagger(builder.Services);

// 🔹 Configure CORS dynamically
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

// DB Seed background service
builder.Services.AddHostedService<DbSeedHostedService>();

var app = builder.Build();

// --------------------
// Middleware Pipeline
// --------------------
app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// ✅ P0 FIX: Enable rate limiting
app.UseRateLimiter();

app.UseSecurityHeaders();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<TenantContextMiddleware>();
app.UseAuthorization();

// --------------------
// API Grouping
// --------------------
var api = app.MapGroup("/api");
api.MapAllEndpoints();

// --------------------
// Swagger
// --------------------
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "NabdCare API v1");
    options.RoutePrefix = "swagger";
});

app.Run();