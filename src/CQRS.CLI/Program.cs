using CliFx;
using CQRS.CLI;
using CQRS.CLI.Commands;
using CQRS.CLI.Commands.Inventory;
using Microsoft.Extensions.DependencyInjection;

var settings = CliSettingsResolver.GetSettings();

var services = new ServiceCollection();
services.AddSingleton(settings);
services.AddSingleton<InventoryApiClient>();
services.AddSingleton<CqrsCommandStatusApiClient>();
services.AddTransient<InventoryCommand>();
services.AddTransient<GetInventoryCommand>();
services.AddTransient<CreateInventoryCommand>();
services.AddTransient<RenameInventoryCommand>();
services.AddTransient<AddItemsCommand>();
services.AddTransient<RemoveItemsCommand>();
services.AddTransient<DeactivateInventoryCommand>();
services.AddTransient<CommandStatusCommand>();
var serviceProvider = services.BuildServiceProvider();

return await new CommandLineApplicationBuilder()
    .AddCommandsFromThisAssembly()
    .UseTypeInstantiator(type => serviceProvider.GetRequiredService(type))
    .Build()
    .RunAsync(args);
