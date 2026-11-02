using CLI.Commands;
using CLI.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddSingleton<IGreetingService, GreetingService>()
    .BuildServiceProvider();

var rootCommand = RootCommandFactory.Create(services);

return await rootCommand.Parse(args).InvokeAsync();
