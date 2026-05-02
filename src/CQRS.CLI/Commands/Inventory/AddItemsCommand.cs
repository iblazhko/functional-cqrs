using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CQRS.CLI.Commands.Inventory;

[Command("inventory add-items", Description = "Add items to an inventory")]
public sealed partial class AddItemsCommand(InventoryApiClient client, CqrsCommandStatusApiClient commandStatusClient) : ICommand
{
    [CommandParameter(0, Name = "id", Description = "Inventory ID")]
    public string Id { get; set; } = string.Empty;

    [CommandOption("count", 'c', Description = "Number of items to add")]
    public int Count { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var response = await client.AddItems(Id, Count);
        await commandStatusClient.WaitForCompletion(response.CommandId!.Value, console);
    }
}
