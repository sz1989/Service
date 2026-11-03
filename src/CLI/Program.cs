using CLI.Commands;
using CLI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

using var host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        // JSON logs go to stderr so stdout stays clean for command output.
        .WriteTo.Console(new CompactJsonFormatter(), standardErrorFromLevel: LogEventLevel.Verbose))
    .ConfigureServices(services => services.AddSingleton<IGreetingService, GreetingService>())
    .Build();

var rootCommand = RootCommandFactory.Create(host.Services);

return await rootCommand.Parse(args).InvokeAsync();
