using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CQRS.CLI.Commands.Inventory;

[Command("inventory rename", Description = "Rename an inventory item")]
public sealed partial class RenameInventoryCommand(
    InventoryApiClient client,
    CqrsCommandStatusApiClient commandStatusClient
) : ICommand
{
    [CommandParameter(0, Name = "id", Description = "Inventory ID")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("name", 'n', Description = "New name")]
    public string Name { get; set; } = string.Empty;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var response = await client.RenameInventory(Id, Name);
        await commandStatusClient.WaitForCompletion(response.CommandId!.Value, console);
    }
}
