using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

namespace Service.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // hits a controller/endpoint, an exception is thrown UseExceptionHandler() middleware catches it then calls GlobalExceptionHandler.TryHandleAsync that handler builds the error response 
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.Title = "My Service API";
                options.Theme = ScalarTheme.Purple;
                options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
            });
        }

        app.UseHttpsRedirection();
        app.UseCors(ServiceCollectionExtensions.DefaultCorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        // /health (simple)
        app.MapHealthChecks("/health");

        // /health/details (JSON with entries)
        app.MapHealthChecks("/health/details", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";

                var result = new
                {
                    status = report.Status.ToString(),
                    totalDuration = report.TotalDuration.TotalMilliseconds,
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        duration = e.Value.Duration.TotalMilliseconds,
                        description = e.Value.Description,
                        data = e.Value.Data
                    })
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(result));
            }
        });

        return app;
    }
}
