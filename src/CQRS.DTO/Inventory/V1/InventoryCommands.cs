namespace CQRS.DTO.Inventory.V1;

// ReSharper disable ClassNeverInstantiated.Global

// Commands

public sealed record CreateInventoryCommand : IInventoryCommandDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record RenameInventoryCommand : IInventoryCommandDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string NewName { get; init; } = string.Empty;
}

public sealed record AddItemsToInventoryCommand : IInventoryCommandDto
{
    public string InventoryId { get; init; } = string.Empty;
    public int Count { get; init; } = 0;
}

public sealed record RemoveItemsFromInventoryCommand : IInventoryCommandDto
{
    public string InventoryId { get; init; } = string.Empty;
    public int Count { get; init; } = 0;
}

public sealed record DeactivateInventoryCommand : IInventoryCommandDto
{
    public string InventoryId { get; init; } = string.Empty;
}
