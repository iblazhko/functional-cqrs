using CQRS.Adapters.InMemoryEventStore;
using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Application.Inventory;
using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.EntityIds;
using CQRS.Mapping.Inventory.V1;
using CQRS.Ports.EventStore;
using Shouldly;

namespace CQRS.Application.Tests;

public sealed class InventoryCommandDtoHandlerTests
{
    private readonly IEventStore<InventoryState, IInventoryEvent, IInventoryEventDto> _eventStore =
        new InMemoryEventStoreAdapter<InventoryState, IInventoryEvent, IInventoryEventDto>();

    private readonly InventoryCommandDtoHandler _handler;

    public InventoryCommandDtoHandlerTests()
    {
        _handler = new InventoryCommandDtoHandler(
            _eventStore,
            new InventoryCommandV1Mapper(),
            new InventoryEventStreamStateProjection(),
            new EventStoreInventoryEventMapper()
        );
    }

    private static string ValidId() => EntityId.NewId();

    private static string InvalidId() => "bad-id";

    [Fact]
    public async Task Handle_ValidCreateCommand_ReturnsCompleted()
    {
        var dto = new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" };

        var result = await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        result.IsT0.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ValidCreateCommand_CompletedResultContainsEvents()
    {
        var dto = new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" };

        var result = await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        result.AsT0.NewEvents.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCreateCommand_EmitsInventoryCreatedEvent()
    {
        var dto = new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" };

        var result = await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        result.AsT0.NewEvents.ShouldContain(e => e is InventoryCreated);
    }

    [Fact]
    public async Task Handle_ValidCreateCommand_EventsSavedToEventStore()
    {
        var id = ValidId();
        var dto = new CreateInventoryCommand { InventoryId = id, Name = "Widget" };

        await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        var streamId = InventoryEventStreamId.GetStreamId(
            InventoryId.Create(EntityId.CreateUnsafe(id))
        );
        var streamExists = await _eventStore.Contains(streamId);
        streamExists.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_InvalidCommandDto_ReturnsRejected()
    {
        var dto = new CreateInventoryCommand { InventoryId = InvalidId(), Name = "Widget" };

        var result = await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        result.IsT1.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsRejected()
    {
        var dto = new CreateInventoryCommand { InventoryId = ValidId(), Name = string.Empty };

        var result = await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        result.IsT1.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CreateOnExistingInventory_ReturnsFailed()
    {
        var id = ValidId();
        var dto = new CreateInventoryCommand { InventoryId = id, Name = "Widget" };

        await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        var result = await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        result.IsT2.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_CreateOnExistingInventory_FailedResultContainsAlreadyExistsError()
    {
        var id = ValidId();
        var dto = new CreateInventoryCommand { InventoryId = id, Name = "Widget" };

        await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());
        var result = await _handler.Handle(dto, MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid());

        result.AsT2.Error.ShouldBeOfType<InventoryAggregate.Errors.InventoryAlreadyExists>();
    }

    [Fact]
    public async Task Handle_ValidRenameCommand_ReturnsCompleted()
    {
        var id = ValidId();
        await _handler.Handle(
            new CreateInventoryCommand { InventoryId = id, Name = "Widget" },
            MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid()
        );

        var result = await _handler.Handle(
            new RenameInventoryCommand { InventoryId = id, NewName = "Gadget" },
            MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid()
        );

        result.IsT0.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_DeactivateOnFullMoon_ReturnsFailed()
    {
        var id = ValidId();

        await _handler.Handle(
            new CreateInventoryCommand { InventoryId = id, Name = "Widget" },
            MoonPhase.NewMoon, Guid.NewGuid(), Guid.NewGuid()
        );

        var result = await _handler.Handle(
            new DeactivateInventoryCommand { InventoryId = id },
            MoonPhase.FullMoon, Guid.NewGuid(), Guid.NewGuid()
        );

        result.IsT2.ShouldBeTrue();
        result.AsT2.Error.ShouldBeOfType<InventoryAggregate.Errors.CannotDeactivateWhenMoonIsFull>();
    }
}
