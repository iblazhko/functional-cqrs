namespace CQRS.API.Inventory;

public sealed record AcceptedResponse
{
    public string? InventoryId { get; init; }
    public Guid? CommandId { get; init; }
    public Guid? CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
}

public sealed record CreateInventoryRequest
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record RenameInventoryRequest
{
    public string Name { get; init; } = string.Empty;
}

public sealed record AddItemsToInventoryRequest
{
    public int Count { get; init; }
}

public sealed record RemoveItemsFromInventoryRequest
{
    public int Count { get; init; }
}

public sealed record DeactivateInventoryRequest { }

public sealed record InventoryResponse
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
}
