using CQRS.Ports.ProjectionStore;

namespace CQRS.Projections.ViewModels.Inventory.V1;

public static class InventoryCollection
{
    public static DocumentCollectionId CollectionId { get; } = (DocumentCollectionId)"Inventories";
}
