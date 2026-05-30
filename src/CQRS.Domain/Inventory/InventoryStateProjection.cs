namespace CQRS.Domain.Inventory;

public static class InventoryStateProjection
{
    public static Option<InventoryState> GetInitialState() => None;

    public static Option<InventoryState> Apply(
        Option<InventoryState> state,
        IInventoryEvent inventoryEvent
    ) =>
        inventoryEvent switch
        {
            InventoryCreated evt => Some(new InventoryState(evt.Id, evt.Name, None, evt.IsActive)),
            _ => state.Match(
                Some: existing => Some(ApplyToExisting(existing, inventoryEvent)),
                // Any non-creation event on a non-existent stream is a defensive assertion,
                // not genuine control flow. Not reachable in practice.
                None: () =>
                    throw new NotSupportedException(
                        $"Inventory event {inventoryEvent.GetType().FullName} applied before the inventory was created"
                    )
            ),
        };

    private static InventoryState ApplyToExisting(
        InventoryState state,
        IInventoryEvent inventoryEvent
    ) =>
        inventoryEvent switch
        {
            InventoryRenamed evt => state with { Name = evt.NewName },
            ItemsAddedToInventory evt => state with { Quantity = evt.NewStockQuantity },
            ItemsRemovedFromInventory evt => state with { Quantity = evt.NewStockQuantity },
            InventoryDeactivated => state with { IsActive = false },
            ItemWentInStock => state,
            ItemWentOutOfStock => state,
            // Default branch is a defensive assertion rather than genuine control flow. Not reachable in practice.
            _ => throw new NotSupportedException(
                $"Inventory event is not supported: {inventoryEvent.GetType().FullName}"
            ),
        };
}
