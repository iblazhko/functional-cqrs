namespace CQRS.Domain.Inventory;

public interface IInventoryCommand
{
    public InventoryId Id { get; }
}

public sealed record CreateInventory(InventoryId Id, InventoryName Name) : IInventoryCommand;

public sealed record RenameInventory(InventoryId Id, InventoryName NewName) : IInventoryCommand;

public sealed record AddItemsToInventory(InventoryId Id, PositiveInteger Count) : IInventoryCommand;

public sealed record RemoveItemsFromInventory(InventoryId Id, PositiveInteger Count)
    : IInventoryCommand;

public sealed record DeactivateInventory(InventoryId Id) : IInventoryCommand;
