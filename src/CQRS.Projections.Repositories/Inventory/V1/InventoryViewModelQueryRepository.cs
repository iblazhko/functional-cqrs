using CQRS.Ports.ProjectionStore;
using CQRS.Projections.ViewModels.Inventory.V1;

namespace CQRS.Projections.Repositories.Inventory.V1;

public class InventoryViewModelQueryRepository(IProjectionStore<InventoryViewModel> projectionStore)
{
    public async Task<InventoryViewModel?> GetById(DocumentId documentId)
    {
        var collectionId = InventoryCollection.CollectionId;
        var collection = await projectionStore.OpenDocumentCollection(collectionId);
        var document = await collection.GetById(documentId);
        return document;
    }
}
