namespace CQRS.DTO.Inventory.V1;

// ReSharper disable ClassNeverInstantiated.Global

public sealed record InventoryCreatedEvent : IInventoryEventDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = false;
}

public sealed record InventoryRenamedEvent : IInventoryEventDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string OldName { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;
}

public sealed record ItemsAddedToInventoryEvent : IInventoryEventDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int AddedCount { get; init; } = 0;
    public int OldStockQuantity { get; init; } = 0;
    public int NewStockQuantity { get; init; } = 0;
}

public sealed record ItemsRemovedFromInventoryEvent : IInventoryEventDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int RemovedCount { get; init; } = 0;
    public int OldStockQuantity { get; init; } = 0;
    public int NewStockQuantity { get; init; } = 0;
}

public sealed record ItemWentInStockEvent : IInventoryEventDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; } = 0;
}

public sealed record ItemWentOutOfStockEvent : IInventoryEventDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record InventoryDeactivatedEvent : IInventoryEventDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
