using CLI.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
        services.AddSingleton(sp =>
        {
            var connectionString = context.Configuration.GetConnectionString("Db");
            return string.IsNullOrWhiteSpace(connectionString) ? throw new InvalidOperationException("ConnectionStrings:Db is not configured.") : NpgsqlDataSource.Create(connectionString);
        });
        services.AddTransient<IGreetingService, GreetingService>();
        services.AddTransient<IPeopleService, PeopleService>();
        services.AddCommands();
    })
    .Build();

var rootCommand = RootCommandFactory.Create(host.Services);

return await rootCommand.Parse(args).InvokeAsync();
