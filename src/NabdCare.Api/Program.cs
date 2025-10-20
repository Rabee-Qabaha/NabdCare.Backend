using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using NabdCare.Api.Configurations;
using NabdCare.Api.Extensions;
using NabdCare.Api.Middleware;
using NabdCare.Infrastructure.Persistence.DataSeed;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Bind configurations
builder.Services.Configure<FrontendSettings>(builder.Configuration);

// Add services and application wiring (repositories, services, AutoMapper, validators, seeders)
builder.Services.AddNabdCareServices(builder.Configuration);

// Controllers + JSON enum strings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Centralized model validation response
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

// Authentication, rate limiting, swagger etc. (extension methods used in your project)
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
SwaggerConfig.AddSwagger(builder.Services);

// CORS
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

// Register the DB seed hosted service once (single registration)
// This will create a scope and run the DbSeeder during startup.
builder.Services.AddHostedService<DbSeedHostedService>();

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseSecurityHeaders();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Map API groups
var api = app.MapGroup("/api");
api.MapAllEndpoints();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "NabdCare API v1");
    options.RoutePrefix = "swagger";
});

app.Run();