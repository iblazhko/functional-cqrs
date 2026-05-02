namespace CQRS.Domain.Inventory;

// ReSharper disable NotAccessedPositionalProperty.Global

public interface IInventoryEvent
{
    InventoryId Id { get; }
}

// Inventory Name is mentioned in every domain event
//
// In theory, we could remove this from *domain events*,
// this information can be inferred from the state.
//
// Name should still be mentioned in *DTO events*.

public sealed record InventoryCreated(InventoryId Id, InventoryName Name, bool IsActive)
    : IInventoryEvent;

public sealed record InventoryRenamed(InventoryId Id, InventoryName OldName, InventoryName NewName)
    : IInventoryEvent;

public sealed record ItemsAddedToInventory(
    InventoryId Id,
    InventoryName Name,
    PositiveInteger AddedCount,
    Option<PositiveInteger> OldStockQuantity,
    PositiveInteger NewStockQuantity
) : IInventoryEvent;

public sealed record ItemsRemovedFromInventory(
    InventoryId Id,
    InventoryName Name,
    PositiveInteger RemovedCount,
    PositiveInteger OldStockQuantity,
    Option<PositiveInteger> NewStockQuantity
) : IInventoryEvent;

public sealed record ItemWentInStock(
    InventoryId Id,
    InventoryName Name,
    PositiveInteger StockQuantity
) : IInventoryEvent;

public sealed record ItemWentOutOfStock(InventoryId Id, InventoryName Name) : IInventoryEvent;

public sealed record InventoryDeactivated(InventoryId Id, InventoryName Name) : IInventoryEvent;
