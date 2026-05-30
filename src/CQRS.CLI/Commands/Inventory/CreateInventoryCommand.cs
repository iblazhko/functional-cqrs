using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CQRS.CLI.Commands.Inventory;

[Command("inventory create", Description = "Create a new inventory item")]
public sealed partial class CreateInventoryCommand(
    InventoryApiClient client,
    CqrsCommandStatusApiClient commandStatusClient
) : ICommand
{
    [CommandOption("id", Description = "Inventory ID (auto-generated if omitted)")]
    public string? Id { get; set; }

    [CommandOption("name", 'n', Description = "Inventory name")]
    public string Name { get; set; } = string.Empty;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var response = await client.CreateInventory(Id, Name);
        await console.Output.WriteLineAsync($"InventoryId: {response.InventoryId}");
        await commandStatusClient.WaitForCompletion(response.CommandId!.Value, console);
    }
}
