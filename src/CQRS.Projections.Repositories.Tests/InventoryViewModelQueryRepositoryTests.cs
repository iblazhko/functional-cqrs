using CQRS.Domain.Inventory;
using CQRS.Ports.EventStore;
using CQRS.Projections.Repositories.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using Shouldly;

namespace CQRS.Projections.Repositories.Tests;

internal sealed class InMemoryInventoryReader : IEventStoreProjectionReader<InventoryViewModel>
{
    private readonly Dictionary<string, InventoryViewModel> _store = new();

    public void Seed(InventoryViewModel vm) => _store[vm.Id] = vm;

    public Task<InventoryViewModel?> GetById(string id) =>
        Task.FromResult(_store.GetValueOrDefault(id));
}

public sealed class InventoryViewModelQueryRepositoryTests
{
    private readonly InMemoryInventoryReader _reader = new();

    private InventoryViewModelQueryRepository MakeRepository() => new(_reader);

    private void SeedViewModel(InventoryViewModel vm) => _reader.Seed(vm);

    // --- GetById ---

    [Fact]
    public async Task GetById_DocumentExists_ReturnsViewModel()
    {
        var inventoryId = InventoryId.NewId();
        SeedViewModel(
            new InventoryViewModel
            {
                Id = inventoryId,
                Name = "Widget",
                IsActive = true,
            }
        );

        var result = await MakeRepository().GetById(inventoryId);

        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetById_DocumentExists_ReturnsViewModelWithCorrectId()
    {
        var inventoryId = InventoryId.NewId();
        SeedViewModel(
            new InventoryViewModel
            {
                Id = inventoryId,
                Name = "Widget",
                IsActive = true,
            }
        );

        var result = await MakeRepository().GetById(inventoryId);

        result!.Id.ShouldBe((string)inventoryId);
    }

    [Fact]
    public async Task GetById_DocumentExists_ReturnsViewModelWithCorrectData()
    {
        var inventoryId = InventoryId.NewId();
        SeedViewModel(
            new InventoryViewModel
            {
                Id = inventoryId,
                Name = "Gadget",
                StockQuantity = 42,
                IsActive = false,
            }
        );

        var result = await MakeRepository().GetById(inventoryId);

        result!.Name.ShouldBe("Gadget");
        result.StockQuantity.ShouldBe(42);
        result.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task GetById_DocumentNotFound_ReturnsNull()
    {
        var result = await MakeRepository().GetById(InventoryId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetById_MultipleDocuments_ReturnsCorrectOne()
    {
        var id1 = InventoryId.NewId();
        var id2 = InventoryId.NewId();
        SeedViewModel(new InventoryViewModel { Id = id1, Name = "First" });
        SeedViewModel(new InventoryViewModel { Id = id2, Name = "Second" });

        var result = await MakeRepository().GetById(id2);

        result!.Name.ShouldBe("Second");
    }
}
