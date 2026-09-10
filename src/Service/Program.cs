using Microsoft.Extensions.AI;
using OllamaSharp;
using Service.Extensions;

namespace Service;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        builder.Services.AddSingleton(_ => new OllamaApiClient(
            new Uri(builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434"),
            builder.Configuration["Ollama:Model"] ?? "llama3.2:1b"));
        builder.Services.AddSingleton<IChatClient>(sp => sp.GetRequiredService<OllamaApiClient>());

        builder.Services.AddMcp();
        builder.Services.AddPersistence(builder.Configuration);
        builder.Services.AddErrorHandling();
        builder.Services.AddRedis(builder.Configuration);
        builder.Services.AddBackgroundProcessing();
        builder.Services.AddRateLimiting(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddApiVersioningSupport();
        builder.Services.AddCorsPolicy(builder.Configuration);
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

            await app.RunAsync();
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
