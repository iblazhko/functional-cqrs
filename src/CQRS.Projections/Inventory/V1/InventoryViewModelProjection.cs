using CQRS.Domain.Inventory;
using CQRS.Projections.ViewModels.Inventory.V1;

namespace CQRS.Projections.Inventory.V1;

public static class InventoryViewModelProjection
{
    public static InventoryViewModel Apply(InventoryViewModel vm, IInventoryEvent @event) =>
        @event switch
        {
            InventoryCreated x => vm with
            {
                Id = x.Id,
                Name = x.Name,
                StockQuantity = 0,
                IsActive = x.IsActive,
            },
            InventoryRenamed x => vm with { Name = x.NewName },
            ItemsAddedToInventory x => vm with { StockQuantity = x.NewStockQuantity },
            ItemsRemovedFromInventory x => vm with
            {
                StockQuantity = x.NewStockQuantity.Match(some => (int)some, () => 0),
            },
            InventoryDeactivated x => vm with { IsActive = false },
            ItemWentInStock => vm,
            ItemWentOutOfStock => vm,
            _ => vm,
        };
}
