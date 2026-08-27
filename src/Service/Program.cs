using Service.Extensions;

namespace Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        builder.Services.AddMcp();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddErrorHandling();
        builder.Services.AddRedis(builder.Configuration);
        builder.Services.AddBackgroundProcessing();
        builder.Services.AddRateLimiting(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        builder.Services.AddJwtAuthentication(builder.Configuration);
        builder.Services.AddMachineLearning(builder.Environment);

        var app = builder.Build();

        try
        {
            Log.Information("Starting web host");

            app.UseApiPipeline();
            app.MapHealthEndpoints();
            app.MapMcp("/mcp");

            // example endpoint to demonstrate OpenAPI documentation
            app.MapGet("/widgets/{id}", (int id) => Results.Ok())
                .WithName("GetWidget")
                .WithSummary("Get a widget by id")
                .WithDescription("Returns a single widget or 404 if not found.");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
