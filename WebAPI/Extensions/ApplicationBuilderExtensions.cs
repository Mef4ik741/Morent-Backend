using Scalar.AspNetCore;
using WebAPI.Application.Hubs;

namespace WebAPI.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        // Swagger UI и Scalar (включены во всех окружениях для удобства разработки)
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth API v1");
            c.RoutePrefix = "swagger"; // открывать по /swagger
        });

        // Также доступен OpenAPI документ по /openapi/v1.json
        app.MapOpenApi();
        
        // Add request logging for debugging
        app.Use(async (context, next) =>
        {
            await next();
        });

        // Enable routing BEFORE CORS for SignalR
        app.UseRouting();
        
        // Configure CORS
        app.UseCors("AllowFrontend");
        
        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Map controllers
        app.MapControllers();
        
        // Map SignalR Hubs
        app.MapHub<RentNotificationHub>("/rentNotificationHub");
        app.MapHub<ChatHub>("/chatHub");
        
        // API documentation - Scalar UI по адресу /scalar
        app.MapScalarApiReference(options =>
        {
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json")
                   .WithTitle("Morent API")
                   .WithTheme(ScalarTheme.BluePlanet);
        });

        return app;
    }
}