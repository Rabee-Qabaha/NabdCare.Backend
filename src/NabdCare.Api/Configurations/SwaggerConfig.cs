using Microsoft.OpenApi.Models;

namespace NabdCare.Api.Configurations
{
    public static class SwaggerConfig
    {
        public static void AddSwagger(IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "NabdCare API",
                    Version = "v1",
                    Description = "API documentation for NabdCare"
                });

                // Optional: Enable XML comments for richer docs
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);
            });
        }
    }
}