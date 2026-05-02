using CQRS.Adapters.InMemoryProjectionStore;
using CQRS.Domain.Inventory;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.Inventory.V1;
using CQRS.Projections.Repositories.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using Shouldly;

namespace CQRS.Projections.Repositories.Tests;

public sealed class InventoryViewModelQueryRepositoryTests
{
    private readonly InMemoryProjectionStoreAdapter<InventoryViewModel> _store = new();

    private InventoryViewModelQueryRepository MakeRepository() =>
        new(_store);

    private async Task SeedViewModel(InventoryViewModel vm)
    {
        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        await collection.Update((DocumentId)vm.Id, _ => vm);
    }

    // --- GetById ---

    [Fact]
    public async Task GetById_DocumentExists_ReturnsViewModel()
    {
        var inventoryId = InventoryId.NewId();
        var documentId = InventoryProjectionDocumentId.FromInventoryId(inventoryId);
        var vm = new InventoryViewModel { Id = (string)inventoryId, Name = "Widget", IsActive = true };
        await SeedViewModel(vm);

        var result = await MakeRepository().GetById(documentId);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetById_DocumentExists_ReturnsViewModelWithCorrectId()
    {
        var inventoryId = InventoryId.NewId();
        var documentId = InventoryProjectionDocumentId.FromInventoryId(inventoryId);
        var vm = new InventoryViewModel { Id = (string)inventoryId, Name = "Widget", IsActive = true };
        await SeedViewModel(vm);

        var result = await MakeRepository().GetById(documentId);

        result!.Id.ShouldBe((string)inventoryId);
    }

    [Fact]
    public async Task GetById_DocumentExists_ReturnsViewModelWithCorrectData()
    {
        var inventoryId = InventoryId.NewId();
        var documentId = InventoryProjectionDocumentId.FromInventoryId(inventoryId);
        var vm = new InventoryViewModel { Id = (string)inventoryId, Name = "Gadget", StockQuantity = 42, IsActive = false };
        await SeedViewModel(vm);

        var result = await MakeRepository().GetById(documentId);

        result!.Name.ShouldBe("Gadget");
        result.StockQuantity.ShouldBe(42);
        result.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task GetById_DocumentNotFound_ReturnsNull()
    {
        var documentId = InventoryProjectionDocumentId.FromInventoryId(InventoryId.NewId());

        var result = await MakeRepository().GetById(documentId);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetById_MultipleDocuments_ReturnsCorrectOne()
    {
        var id1 = InventoryId.NewId();
        var id2 = InventoryId.NewId();
        await SeedViewModel(new InventoryViewModel { Id = (string)id1, Name = "First" });
        await SeedViewModel(new InventoryViewModel { Id = (string)id2, Name = "Second" });

        var result = await MakeRepository().GetById(InventoryProjectionDocumentId.FromInventoryId(id2));

        result!.Name.ShouldBe("Second");
    }
}
