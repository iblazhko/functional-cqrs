using CQRS.Adapters.InMemoryProjectionStore;
using CQRS.Domain.Inventory;
using CQRS.DTO.Inventory.V1;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using Shouldly;

namespace CQRS.Projections.Tests;

public sealed class InventoryEventDtoHandlerTests
{
    private readonly InMemoryProjectionStoreAdapter<InventoryViewModel> _store = new();

    private IInventoryDomainEventDtoHandlerContext<InventoryViewModel> MakeContext() =>
        new InventoryInventoryDomainEventDtoHandlerContext<InventoryViewModel>(
            _store,
            (vm, evt) => InventoryViewModelProjection.Apply(vm, evt)
        );

    private static string ValidId() => (string)InventoryId.NewId();

    private static string InvalidId() => "bad-id";

    // --- Happy path ---

    [Fact]
    public async Task Handle_ValidDto_ReturnsRight()
    {
        var dto = new InventoryCreatedEvent { InventoryId = ValidId(), Name = "Widget", IsActive = true };

        var result = await InventoryEventDtoHandler.Handle(dto, MakeContext());

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidDto_ViewModelStoredInProjectionStore()
    {
        var id = ValidId();
        var dto = new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true };

        await InventoryEventDtoHandler.Handle(dto, MakeContext());

        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        var stored = await collection.GetById((DocumentId)id);
        stored.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_InventoryCreatedDto_ViewModelHasCorrectName()
    {
        var id = ValidId();
        var dto = new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true };

        await InventoryEventDtoHandler.Handle(dto, MakeContext());

        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        var stored = await collection.GetById((DocumentId)id);
        stored!.Name.ShouldBe("Widget");
    }

    [Fact]
    public async Task Handle_InventoryCreatedDto_ViewModelIsActive()
    {
        var id = ValidId();
        var dto = new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true };

        await InventoryEventDtoHandler.Handle(dto, MakeContext());

        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        var stored = await collection.GetById((DocumentId)id);
        stored!.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_SequentialDtos_AccumulatesViewModelState()
    {
        var id = ValidId();
        var context = MakeContext();

        await InventoryEventDtoHandler.Handle(
            new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true },
            context
        );
        await InventoryEventDtoHandler.Handle(
            new InventoryRenamedEvent { InventoryId = id, OldName = "Widget", NewName = "Gadget" },
            context
        );

        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        var stored = await collection.GetById((DocumentId)id);
        stored!.Name.ShouldBe("Gadget");
    }

    [Fact]
    public async Task Handle_AddItemsDtoAfterCreate_UpdatesStockQuantity()
    {
        var id = ValidId();
        var context = MakeContext();

        await InventoryEventDtoHandler.Handle(
            new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true },
            context
        );
        await InventoryEventDtoHandler.Handle(
            new ItemsAddedToInventoryEvent { InventoryId = id, Name = "Widget", AddedCount = 5, OldStockQuantity = 0, NewStockQuantity = 5 },
            context
        );

        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        var stored = await collection.GetById((DocumentId)id);
        stored!.StockQuantity.ShouldBe(5);
    }

    [Fact]
    public async Task Handle_InventoryDeactivatedDto_SetsIsActiveFalse()
    {
        var id = ValidId();
        var context = MakeContext();

        await InventoryEventDtoHandler.Handle(
            new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true },
            context
        );
        await InventoryEventDtoHandler.Handle(
            new InventoryDeactivatedEvent { InventoryId = id, Name = "Widget" },
            context
        );

        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        var stored = await collection.GetById((DocumentId)id);
        stored!.IsActive.ShouldBeFalse();
    }

    // --- Mapping failure ---

    [Fact]
    public async Task Handle_InvalidInventoryId_ReturnsLeft()
    {
        var dto = new InventoryCreatedEvent { InventoryId = InvalidId(), Name = "Widget" };

        var result = await InventoryEventDtoHandler.Handle(dto, MakeContext());

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsLeft()
    {
        var dto = new InventoryCreatedEvent { InventoryId = ValidId(), Name = string.Empty };

        var result = await InventoryEventDtoHandler.Handle(dto, MakeContext());

        result.IsLeft.ShouldBeTrue();
    }
}
