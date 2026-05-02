using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CQRS.CLI.Commands.Inventory;

[Command("inventory", Description = "Inventory management commands")]
public sealed partial class InventoryCommand : ICommand, ICommandWithHelpOption
{
    [CommandOption("help", 'h', Description = "Show help text")]
    public bool IsHelpRequested { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await console.Output.WriteLineAsync(
            "Usage: inventory <command> [options]"
            + "\nRun 'inventory --help' to list available commands."
        );
    }
}
