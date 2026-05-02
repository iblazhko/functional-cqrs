using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CQRS.CLI.Commands.Inventory;

[Command("inventory deactivate", Description = "Deactivate an inventory item")]
public sealed partial class DeactivateInventoryCommand(InventoryApiClient client, CqrsCommandStatusApiClient commandStatusClient) : ICommand
{
    [CommandParameter(0, Name = "id", Description = "Inventory ID")]
    public string Id { get; set; } = string.Empty;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var response = await client.Deactivate(Id);
        await commandStatusClient.WaitForCompletion(response.CommandId!.Value, console);
    }
}
