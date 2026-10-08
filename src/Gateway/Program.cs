using Gateway.Extensions;
using Serilog;

namespace Gateway;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

        builder.Services.AddCorsPolicy(builder.Configuration);
        builder.Services.AddGatewayAuthentication(builder.Configuration);
        builder.Services.AddGatewayRateLimiting(builder.Configuration);
        builder.Services.AddYarpGateway(builder.Configuration);
        builder.Services.AddHealthChecks();

        var app = builder.Build();

        try
        {
            Log.Information("Starting gateway host");

            app.UseGatewayPipeline();
            app.MapHealthChecks("/health");
            app.MapReverseProxy();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Gateway terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
