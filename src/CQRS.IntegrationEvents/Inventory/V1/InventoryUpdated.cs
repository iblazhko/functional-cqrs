namespace CQRS.IntegrationEvents.Inventory.V1;

public sealed record InventoryUpdated
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; } = 0;
    public bool IsActive { get; init; } = true;
}
