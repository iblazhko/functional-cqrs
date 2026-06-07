using CQRS.Ports.EventStore;
using CQRS.Projections.ViewModels.Inventory.V1;

namespace CQRS.Projections.Repositories.Inventory.V1;

public class InventoryViewModelQueryRepository(
    IEventStoreProjectionReader<InventoryViewModel> reader
)
{
    public Task<InventoryViewModel?> GetById(string inventoryId) => reader.GetById(inventoryId);
}
