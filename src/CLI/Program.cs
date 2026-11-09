using CLI.Commands;
using CLI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

using var host = Host.CreateDefaultBuilder(args)
    // Look for appsettings.json next to the executable, not in the caller's working directory (single-file friendly).
    .UseContentRoot(AppContext.BaseDirectory)
    .UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        // JSON logs go to stderr so stdout stays clean for command output.
        .WriteTo.Console(new CompactJsonFormatter(), standardErrorFromLevel: LogEventLevel.Verbose))
    .ConfigureServices((context, services) =>
    {
        _ = services.AddSingleton(sp =>
        {
            var connectionString = context.Configuration.GetConnectionString("Db");
            return string.IsNullOrWhiteSpace(connectionString) ? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.") : NpgsqlDataSource.Create(connectionString);
        });
        _ = services.AddTransient<IGreetingService, GreetingService>();
        _ = services.AddTransient<IPeopleService, PeopleService>();
    })
    .Build();

var rootCommand = RootCommandFactory.Create(host.Services);

return await rootCommand.Parse(args).InvokeAsync();
