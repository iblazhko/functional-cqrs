using CQRS.Adapters.InMemoryProjectionStore;
using CQRS.API.Inventory;
using CQRS.EntityIds;
using CQRS.Mapping;
using CQRS.Mapping.Inventory.V1;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.Repositories.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using LanguageExt;
using Shouldly;

namespace CQRS.API.Tests;

public sealed class InventoryApiServiceTests
{
    private readonly FakeMessageBus _bus = new();
    private readonly InMemoryProjectionStoreAdapter<InventoryViewModel> _store = new();
    private readonly InventoryViewModelQueryRepository _queryRepository;
    private readonly InventoryApiService _service;

    public InventoryApiServiceTests()
    {
        _queryRepository = new InventoryViewModelQueryRepository(_store);
        _service = new InventoryApiService(
            _bus,
            new InventoryCommandV1Mapper(),
            _queryRepository,
            TimeProvider.System
        );
    }

    private static string ValidId() => EntityId.NewId();

    private static string InvalidId() => "bad-id";

    private async Task SeedInventory(
        string inventoryId,
        string name,
        int stock = 0,
        bool isActive = true
    )
    {
        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        await collection.Update(
            (DocumentId)inventoryId,
            new InventoryViewModel
            {
                Id = inventoryId,
                Name = name,
                StockQuantity = stock,
                IsActive = isActive,
            }
        );
    }

    // --- GetInventory ---

    [Fact]
    public async Task GetInventory_ExistingId_ReturnsSomeWithCorrectFields()
    {
        var id = ValidId();
        await SeedInventory(id, "Widget", 10, true);

        var result = await _service.GetInventory(id);

        var response = result.Match(r => r, () => null!);
        response.ShouldNotBeNull();
        response.InventoryId.ShouldBe(id.ToUpperInvariant());
        response.Name.ShouldBe("Widget");
        response.StockQuantity.ShouldBe(10);
        response.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetInventory_UnknownId_ReturnsNone()
    {
        var result = await _service.GetInventory(ValidId());

        result.IsNone.ShouldBeTrue();
    }

    // --- CreateInventory ---

    [Fact]
    public async Task CreateInventory_ValidRequest_ReturnsRight()
    {
        var id = ValidId();
        var request = new CreateInventoryRequest { InventoryId = id, Name = "New Inventory" };

        var result = await _service.CreateInventory(request);

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateInventory_ValidRequest_SendsCommandToBus()
    {
        var id = ValidId();
        var request = new CreateInventoryRequest { InventoryId = id, Name = "New Inventory" };

        await _service.CreateInventory(request);

        _bus.SentMessages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreateInventory_ValidRequest_AcceptedResponseContainsInventoryId()
    {
        var id = ValidId();
        var request = new CreateInventoryRequest { InventoryId = id, Name = "New Inventory" };

        var result = await _service.CreateInventory(request);

        var response = result.IfLeft((AcceptedResponse)null!);
        response.ShouldNotBeNull();
        response.InventoryId.ShouldBe(id.ToUpperInvariant());
    }

    [Fact]
    public async Task CreateInventory_ValidRequest_AcceptedResponseHasCorrelationAndCausationIds()
    {
        var request = new CreateInventoryRequest { InventoryId = ValidId(), Name = "Widget" };

        var result = await _service.CreateInventory(request);

        var response = result.IfLeft((AcceptedResponse)null!);
        response.ShouldNotBeNull();
        response.CorrelationId.ShouldNotBeNull();
        response.CausationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateInventory_EmptyInventoryId_AutoGeneratesId()
    {
        var request = new CreateInventoryRequest { InventoryId = string.Empty, Name = "Auto ID" };

        var result = await _service.CreateInventory(request);

        result.IsRight.ShouldBeTrue();
        var response = result.IfLeft((AcceptedResponse)null!);
        response.ShouldNotBeNull();
        response.InventoryId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateInventory_EmptyName_ReturnsLeft()
    {
        var request = new CreateInventoryRequest { InventoryId = ValidId(), Name = string.Empty };

        var result = await _service.CreateInventory(request);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateInventory_InvalidId_ReturnsLeft()
    {
        var request = new CreateInventoryRequest { InventoryId = InvalidId(), Name = "Widget" };

        var result = await _service.CreateInventory(request);

        result.IsLeft.ShouldBeTrue();
    }

    // --- RenameInventory ---

    [Fact]
    public async Task RenameInventory_ValidRequest_ReturnsRight()
    {
        var result = await _service.RenameInventory(
            ValidId(),
            new RenameInventoryRequest { Name = "New Name" }
        );

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RenameInventory_ValidRequest_SendsCommandToBus()
    {
        await _service.RenameInventory(ValidId(), new RenameInventoryRequest { Name = "New Name" });

        _bus.SentMessages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RenameInventory_EmptyName_ReturnsLeft()
    {
        var result = await _service.RenameInventory(
            ValidId(),
            new RenameInventoryRequest { Name = string.Empty }
        );

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task RenameInventory_InvalidId_ReturnsLeft()
    {
        var result = await _service.RenameInventory(
            InvalidId(),
            new RenameInventoryRequest { Name = "Valid" }
        );

        result.IsLeft.ShouldBeTrue();
    }

    // --- AddItemsToInventory ---

    [Fact]
    public async Task AddItemsToInventory_ValidRequest_ReturnsRight()
    {
        var result = await _service.AddItemsToInventory(
            ValidId(),
            new AddItemsToInventoryRequest { Count = 5 }
        );

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task AddItemsToInventory_ValidRequest_SendsCommandToBus()
    {
        await _service.AddItemsToInventory(ValidId(), new AddItemsToInventoryRequest { Count = 5 });

        _bus.SentMessages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task AddItemsToInventory_ZeroCount_ReturnsLeft()
    {
        var result = await _service.AddItemsToInventory(
            ValidId(),
            new AddItemsToInventoryRequest { Count = 0 }
        );

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task AddItemsToInventory_NegativeCount_ReturnsLeft()
    {
        var result = await _service.AddItemsToInventory(
            ValidId(),
            new AddItemsToInventoryRequest { Count = -1 }
        );

        result.IsLeft.ShouldBeTrue();
    }

    // --- RemoveItemsFromInventory ---

    [Fact]
    public async Task RemoveItemsFromInventory_ValidRequest_ReturnsRight()
    {
        var result = await _service.RemoveItemsFromInventory(
            ValidId(),
            new RemoveItemsFromInventoryRequest { Count = 3 }
        );

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveItemsFromInventory_ValidRequest_SendsCommandToBus()
    {
        await _service.RemoveItemsFromInventory(
            ValidId(),
            new RemoveItemsFromInventoryRequest { Count = 3 }
        );

        _bus.SentMessages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task RemoveItemsFromInventory_ZeroCount_ReturnsLeft()
    {
        var result = await _service.RemoveItemsFromInventory(
            ValidId(),
            new RemoveItemsFromInventoryRequest { Count = 0 }
        );

        result.IsLeft.ShouldBeTrue();
    }

    // --- DeactivateInventory ---

    [Fact]
    public async Task DeactivateInventory_ValidRequest_ReturnsRight()
    {
        var result = await _service.DeactivateInventory(
            ValidId(),
            new DeactivateInventoryRequest()
        );

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task DeactivateInventory_ValidRequest_SendsCommandToBus()
    {
        await _service.DeactivateInventory(ValidId(), new DeactivateInventoryRequest());

        _bus.SentMessages.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task DeactivateInventory_InvalidId_ReturnsLeft()
    {
        var result = await _service.DeactivateInventory(
            InvalidId(),
            new DeactivateInventoryRequest()
        );

        result.IsLeft.ShouldBeTrue();
    }
}
