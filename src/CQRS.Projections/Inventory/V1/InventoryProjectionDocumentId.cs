using CQRS.Domain.Inventory;
using CQRS.Ports.ProjectionStore;

namespace CQRS.Projections.Inventory.V1;

public static class InventoryProjectionDocumentId
{
    public static DocumentId FromInventoryId(InventoryId inventoryId)
    {
        var idStr = (string)inventoryId;
        return (DocumentId)idStr;
    }
}
