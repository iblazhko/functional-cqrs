namespace CQRS.Projections.ViewModels.Inventory.V1;

public record InventoryViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; } = 0;
    public bool IsActive { get; init; } = true;
}
