namespace CQRS.Domain.Inventory;

public static class InventoryStateProjection
{
    public static InventoryState GetInitialState(InventoryId inventoryId) =>
        new(inventoryId, InventoryName.Create(MediumString.CreateUnsafe("N/A")), None, true, true);

    public static InventoryState Apply(InventoryState state, IInventoryEvent inventoryEvent) =>
        inventoryEvent switch
        {
            InventoryCreated evt => new InventoryState(evt.Id, evt.Name, None, false, evt.IsActive),
            InventoryRenamed evt => state with { Name = evt.NewName },
            ItemsAddedToInventory evt => state with { Quantity = evt.NewStockQuantity },
            ItemsRemovedFromInventory evt => state with { Quantity = evt.NewStockQuantity },
            InventoryDeactivated => state with { IsActive = false },
            ItemWentInStock => state,
            ItemWentOutOfStock => state,
            // Default branch is a defensive assertion rather than genuine control flow. Not reachable in practice.
            _ => throw new NotSupportedException($"Inventory event is not supported: {inventoryEvent.GetType().FullName}"),
        };
}
