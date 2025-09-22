using NabdCare.Api.Configurations;
using NabdCare.Api.EndPoints;
using NabdCare.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddNabdCareServices(builder.Configuration);

// Add Swagger via config class
SwaggerConfig.AddSwagger(builder.Services);

var app = builder.Build();

// Register your endpoints:
app.MapAuthEndpoints();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "NabdCare API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseMiddleware<TenantContextMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();