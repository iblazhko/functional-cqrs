namespace CQRS.DTO.Inventory.V1;

public sealed class InventoryStateDto : ICqrsStateDto
{
    public string InventoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int StockQuantity { get; init; } = 0;
    public bool IsNew { get; init; } = false;
    public bool IsActive { get; init; } = false;
}
