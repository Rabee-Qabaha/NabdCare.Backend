using System.Text.Json.Serialization;
using NabdCare.Api.Configurations;
using NabdCare.Api.Extensions;
using NabdCare.Api.Middleware;
using NabdCare.Infrastructure.Persistence.DataSeed;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// Add services
// --------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // ✅ Serialize enums as strings globally
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddNabdCareServices(builder.Configuration);

// Swagger
SwaggerConfig.AddSwagger(builder.Services);

// CORS
var allowedOrigins = builder.Environment.IsDevelopment()
    ? new[] { "http://localhost:5174", "https://localhost:5174" }
    : builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

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