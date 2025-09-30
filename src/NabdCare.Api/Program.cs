using NabdCare.Api.Configurations;
using NabdCare.Api.Endpoints;
using NabdCare.Api.Middleware;
using NabdCare.Infrastructure.Persistence.DataSeed;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddNabdCareServices(builder.Configuration);
SwaggerConfig.AddSwagger(builder.Services);

// Register the hosted service to seed database
builder.Services.AddHostedService<DbSeedHostedService>();

var app = builder.Build();

// Configure middleware pipeline

// Error handling first
app.UseMiddleware<ErrorHandlingMiddleware>();

// HTTPS redirection
app.UseHttpsRedirection();

// Authentication must come before tenant context
app.UseAuthentication();

// Then tenant context to read claims
app.UseMiddleware<TenantContextMiddleware>();

// Authorization after tenant context
app.UseAuthorization();

// Map your endpoints
app.MapAuthEndpoints();
app.MapPermissionEndpoints();
app.MapUserEndpoints();
app.MapClinicEndpoints();
app.MapControllers();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "NabdCare API v1");
    options.RoutePrefix = "swagger";
});

app.Run();