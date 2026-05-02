using CQRS.Adapters.InMemoryProjectionStore;
using CQRS.DTO.Inventory.V1;
using CQRS.EntityIds;
using CQRS.Mapping;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.ViewModels.Inventory.V1;
using CQRS.Projections.WolverineHandlers;
using Shouldly;

namespace CQRS.Projections.WolverineHandlers.Tests;

public sealed class InventoryEventConsumerTests
{
    private readonly InMemoryProjectionStoreAdapter<InventoryViewModel> _store = new();
    private readonly InventoryEventConsumer _consumer;

    public InventoryEventConsumerTests()
    {
        _consumer = new InventoryEventConsumer(_store);
    }

    private static string ValidId() => (string)EntityId.NewId();

    private Task SeedCreated(string id, string name = "Widget") =>
        _consumer.Consume(new InventoryCreatedEvent { InventoryId = id, Name = name, IsActive = true });

    private async Task<InventoryViewModel?> GetViewModel(string id)
    {
        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        return await collection.GetById((DocumentId)id);
    }

    // --- InventoryCreatedEvent ---

    [Fact]
    public async Task Consume_InventoryCreatedEvent_StoresViewModelInProjectionStore()
    {
        var id = ValidId();
        await _consumer.Consume(new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true });
        (await GetViewModel(id)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Consume_InventoryCreatedEvent_ViewModelReflectsEventFields()
    {
        var id = ValidId();
        await _consumer.Consume(new InventoryCreatedEvent { InventoryId = id, Name = "Widget", IsActive = true });

        var vm = await GetViewModel(id);
        vm!.Name.ShouldBe("Widget");
        vm.IsActive.ShouldBeTrue();
        vm.StockQuantity.ShouldBe(0);
    }

    // --- InventoryRenamedEvent ---

    [Fact]
    public async Task Consume_InventoryRenamedEvent_UpdatesName()
    {
        var id = ValidId();
        await SeedCreated(id, "Widget");

        await _consumer.Consume(new InventoryRenamedEvent { InventoryId = id, OldName = "Widget", NewName = "Gadget" });

        (await GetViewModel(id))!.Name.ShouldBe("Gadget");
    }

    // --- ItemsAddedToInventoryEvent ---

    [Fact]
    public async Task Consume_ItemsAddedToInventoryEvent_UpdatesStockQuantity()
    {
        var id = ValidId();
        await SeedCreated(id);

        await _consumer.Consume(new ItemsAddedToInventoryEvent
        {
            InventoryId = id, Name = "Widget",
            AddedCount = 10, OldStockQuantity = 0, NewStockQuantity = 10,
        });

        (await GetViewModel(id))!.StockQuantity.ShouldBe(10);
    }

    // --- ItemsRemovedFromInventoryEvent ---

    [Fact]
    public async Task Consume_ItemsRemovedFromInventoryEvent_UpdatesStockQuantity()
    {
        var id = ValidId();
        await SeedCreated(id);
        await _consumer.Consume(new ItemsAddedToInventoryEvent
        {
            InventoryId = id, Name = "Widget",
            AddedCount = 10, OldStockQuantity = 0, NewStockQuantity = 10,
        });

        await _consumer.Consume(new ItemsRemovedFromInventoryEvent
        {
            InventoryId = id, Name = "Widget",
            RemovedCount = 3, OldStockQuantity = 10, NewStockQuantity = 7,
        });

        (await GetViewModel(id))!.StockQuantity.ShouldBe(7);
    }

    // --- InventoryDeactivatedEvent ---

    [Fact]
    public async Task Consume_InventoryDeactivatedEvent_SetsIsActiveFalse()
    {
        var id = ValidId();
        await SeedCreated(id);

        await _consumer.Consume(new InventoryDeactivatedEvent { InventoryId = id, Name = "Widget" });

        (await GetViewModel(id))!.IsActive.ShouldBeFalse();
    }

    // --- ItemWentInStockEvent / ItemWentOutOfStockEvent ---
    // These events don't mutate the view model (pass-through in projection),
    // so the only contract to verify is that no exception is thrown.

    [Fact]
    public async Task Consume_ItemWentInStockEvent_DoesNotThrow()
    {
        var id = ValidId();
        await SeedCreated(id);

        await Should.NotThrowAsync(
            () => _consumer.Consume(new ItemWentInStockEvent { InventoryId = id, Name = "Widget", StockQuantity = 1 })
        );
    }

    [Fact]
    public async Task Consume_ItemWentOutOfStockEvent_DoesNotThrow()
    {
        var id = ValidId();
        await SeedCreated(id);

        await Should.NotThrowAsync(
            () => _consumer.Consume(new ItemWentOutOfStockEvent { InventoryId = id, Name = "Widget" })
        );
    }

    // --- Error path: mapping failure → MappingException ---

    [Fact]
    public async Task Consume_WithInvalidInventoryId_ThrowsMappingException()
    {
        // "bad-id" fails EntityId.Create validation → Left<MappingFault> → MappingException
        await Should.ThrowAsync<MappingException>(
            () => _consumer.Consume(new InventoryCreatedEvent { InventoryId = "bad-id", Name = "Widget", IsActive = true })
        );
    }

    [Fact]
    public async Task Consume_WithEmptyName_ThrowsMappingException()
    {
        await Should.ThrowAsync<MappingException>(
            () => _consumer.Consume(new InventoryCreatedEvent { InventoryId = ValidId(), Name = string.Empty, IsActive = true })
        );
    }

    [Fact]
    public async Task Consume_WithInvalidInventoryId_DoesNotUpdateProjectionStore()
    {
        await Should.ThrowAsync<MappingException>(
            () => _consumer.Consume(new InventoryCreatedEvent { InventoryId = "bad-id", Name = "Widget", IsActive = true })
        );

        var collection = await _store.OpenDocumentCollection(InventoryCollection.CollectionId);
        (await collection.GetById((DocumentId)"bad-id")).ShouldBeNull();
    }
}
