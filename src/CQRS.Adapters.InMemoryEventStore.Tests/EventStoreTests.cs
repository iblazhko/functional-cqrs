using CQRS.Application.Inventory;
using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.Ports.EventStore;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace CQRS.Adapters.InMemoryEventStore.Tests;

public sealed class EventStoreTests
{
#pragma warning disable CA1859
    private readonly IEventStore<
        Option<InventoryState>,
        IInventoryEvent,
        IInventoryEventDto
    > _eventStore =
#pragma warning restore CA1859
        new InMemoryEventStoreAdapter<
            Option<InventoryState>,
            IInventoryEvent,
            IInventoryEventDto
        >();

    private readonly IEventMapper<IInventoryEvent, IInventoryEventDto> _eventMapper =
        new EventStoreInventoryEventMapper();

    [Fact]
    public async Task Open_WithNonExistingStreamId_ShouldCreateSessionWithEmptyStream()
    {
        var entityId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(entityId);
        var session = _eventStore.Open(streamId, _eventMapper);
        session.ShouldNotBeNull();

        var eventStream = await session.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );
        eventStream.ShouldNotBeNull();
        eventStream.StreamId.ShouldBe(streamId);
        eventStream.StreamVersion.ShouldBe(EventStreamVersion.InitialVersion);
        eventStream.Events.ShouldNotBeNull();
        eventStream.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AppendEvents_WithEntityEvents_ShouldAppendEventsToSession()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-123");

        var streamId = InventoryEventStreamId.GetStreamId(entityId);
        var session = _eventStore.Open(streamId, _eventMapper);

        List<IInventoryEvent> eventsToAppend =
        [
            new InventoryCreated(entityId, entityName, true),
            new ItemsAddedToInventory(
                entityId,
                entityName,
                PositiveInteger.CreateUnsafe(1),
                None,
                PositiveInteger.CreateUnsafe(1)
            ),
        ];

        session.AppendEvents(eventsToAppend, Guid.NewGuid(), Guid.NewGuid());

        var allEvents = await session.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );
        allEvents.ShouldNotBeNull();
        allEvents.StreamId.ShouldBe(streamId);
        allEvents.StreamVersion.ShouldBe((EventStreamVersion)2);
        allEvents.Events.ShouldNotBeNull();
        allEvents.Events.Select(x => x.Event).ToList().ShouldBeEquivalentTo(eventsToAppend);

        var newEvents = session.GetNewEvents();
        newEvents.ShouldBeEquivalentTo(allEvents);
    }

    [Fact]
    public async Task Save_WithNonEmptySession_ShouldSaveEvents()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-123");

        var streamId = InventoryEventStreamId.GetStreamId(entityId);
        var session = _eventStore.Open(streamId, _eventMapper);

        List<IInventoryEvent> eventsToAppend =
        [
            new InventoryCreated(entityId, entityName, true),
            new ItemsAddedToInventory(
                entityId,
                entityName,
                PositiveInteger.CreateUnsafe(1),
                None,
                PositiveInteger.CreateUnsafe(1)
            ),
        ];

        session.AppendEvents(eventsToAppend, Guid.NewGuid(), Guid.NewGuid());
        await session.Save(cancellationToken: TestContext.Current.CancellationToken);

        var session2 = _eventStore.Open(streamId, _eventMapper);
        var allEvents = await session2.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );
        allEvents.Events.Select(x => x.Event).ToList().ShouldBeEquivalentTo(eventsToAppend);
    }

    [Fact]
    public async Task Contains_ForNewStream_ReturnsFalse()
    {
        var entityId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        var result = await _eventStore.Contains(
            streamId,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Contains_AfterSavingEvents_ReturnsTrue()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-CONTAINS");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        using var session = _eventStore.Open(streamId, _eventMapper);
        session.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session.Save(cancellationToken: TestContext.Current.CancellationToken);

        var result = await _eventStore.Contains(
            streamId,
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task GetState_EmptyStream_ReturnsInitialState()
    {
        var entityId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(entityId);
        var projection = new InventoryEventStreamStateProjection();

        using var session = _eventStore.Open(streamId, _eventMapper);
        var result = await session.GetState(
            projection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.IsRight.ShouldBeTrue();
        var state = result.Match(Left: _ => Option<InventoryState>.None, Right: s => s);
        state.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetState_WithSavedEvents_ReturnsProjectedState()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-STATE");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);
        var projection = new InventoryEventStreamStateProjection();

        using var session1 = _eventStore.Open(streamId, _eventMapper);
        session1.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);

        using var session2 = _eventStore.Open(streamId, _eventMapper);
        var result = await session2.GetState(
            projection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.IsRight.ShouldBeTrue();
        var state = result.Match(Left: _ => Option<InventoryState>.None, Right: s => s);
        state.IsSome.ShouldBeTrue();
        state.Map(s => s.Name).ShouldBe(Some(entityName));
    }

    [Fact]
    public async Task GetAllEvents_AfterSave_ThrowsSessionIsLockedException()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-LOCK");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        var session = _eventStore.Open(streamId, _eventMapper);
        session.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session.Save(cancellationToken: TestContext.Current.CancellationToken);

        await Should.ThrowAsync<SessionIsLockedException>(async () => await session.GetAllEvents());
    }

    [Fact]
    public async Task AppendEvents_WithEventWithMetadata_ShouldAppendEventsToSession()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-META");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        using var session = _eventStore.Open(streamId, _eventMapper);
        var domainEvent = new InventoryCreated(entityId, entityName, true);
        var eventWithMetadata = EventWithMetadata<IInventoryEvent>.FromEvent(
            domainEvent,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        session.AppendEvents(new[] { eventWithMetadata });

        var allEvents = await session.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );
        allEvents.Events.Count.ShouldBe(1);
        allEvents.Events.Single().Event.ShouldBe(domainEvent);
    }

    [Fact]
    public async Task GetNewEvents_AfterLoadingExistingEvents_ReturnsOnlyNewlyAppendedEvents()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-NEWEVT");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        using var session1 = _eventStore.Open(streamId, _eventMapper);
        session1.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);

        using var session2 = _eventStore.Open(streamId, _eventMapper);
        var newDomainEvent = new ItemsAddedToInventory(
            entityId,
            entityName,
            PositiveInteger.CreateUnsafe(5),
            None,
            PositiveInteger.CreateUnsafe(5)
        );
        session2.AppendEvents([newDomainEvent], null, null);

        var newEvents = session2.GetNewEvents();
        newEvents.Events.Count.ShouldBe(1);
        newEvents.Events.Single().Event.ShouldBe(newDomainEvent);
    }

    [Fact]
    public async Task GetNewEvents_OnFreshSession_ReturnsEmptyStream()
    {
        var entityId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        using var session = _eventStore.Open(streamId, _eventMapper);
        var newEvents = session.GetNewEvents();

        newEvents.Events.ShouldBeEmpty();
        newEvents.StreamVersion.ShouldBe(EventStreamVersion.InitialVersion);
    }

    [Fact]
    public async Task GetAllEvents_WithPreLoadedAndNewEvents_ReturnsCombinedEvents()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-COMBINED");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        var preLoadedEvent = new InventoryCreated(entityId, entityName, true);
        using var session1 = _eventStore.Open(streamId, _eventMapper);
        session1.AppendEvents([preLoadedEvent], null, null);
        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);

        using var session2 = _eventStore.Open(streamId, _eventMapper);
        var newEvent = new ItemsAddedToInventory(
            entityId,
            entityName,
            PositiveInteger.CreateUnsafe(3),
            None,
            PositiveInteger.CreateUnsafe(3)
        );
        session2.AppendEvents([newEvent], null, null);

        var allEvents = await session2.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );

        var events = allEvents.Events.ToList();
        events.Count.ShouldBe(2);
        events[0].Event.ShouldBe(preLoadedEvent);
        events[1].Event.ShouldBe(newEvent);
    }

    [Fact]
    public async Task GetState_WithLoadedAndUnsavedNewEvents_ReflectsAllEvents()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-UNSAVED");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);
        var projection = new InventoryEventStreamStateProjection();

        using var session1 = _eventStore.Open(streamId, _eventMapper);
        session1.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);

        using var session2 = _eventStore.Open(streamId, _eventMapper);
        var updatedName = InventoryName.CreateUnsafe("INV-RENAMED");
        session2.AppendEvents(
            [new InventoryRenamed(entityId, entityName, updatedName)],
            null,
            null
        );

        var result = await session2.GetState(
            projection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.IsRight.ShouldBeTrue();
        result
            .Match(Left: _ => Option<InventoryState>.None, Right: s => s)
            .Map(s => s.Name)
            .ShouldBe(Some(updatedName));
    }

    [Fact]
    public async Task ConcurrentSave_ThrowsConcurrencyException()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-CONC");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        var session1 = _eventStore.Open(streamId, _eventMapper);
        var session2 = _eventStore.Open(streamId, _eventMapper);

        session1.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        session2.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);

        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);
        await Should.ThrowAsync<ConcurrencyException>(async () => await session2.Save());
    }
}
