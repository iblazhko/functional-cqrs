using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CQRS.CLI.Commands.Inventory;

[Command("inventory get", Description = "Get an inventory item by ID")]
public sealed partial class GetInventoryCommand(InventoryApiClient client) : ICommand
{
    [CommandParameter(0, Name = "id", Description = "Inventory ID")]
    public string Id { get; set; } = string.Empty;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var inventory = await client.GetInventory(Id);

        if (inventory is null)
            throw new CommandException($"Inventory '{Id}' not found.");

        await console.Output.WriteLineAsync(
            $"""
             Inventory:  {inventory.InventoryId}");
               Name:     {inventory.Name}");
               Stock:    {inventory.StockQuantity}");
               Active:   {(inventory.IsActive ? "Yes" : "No")}");
             """);
    }
}
