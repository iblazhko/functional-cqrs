using CQRS.Application.Inventory;
using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.Ports.EventStore;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace CQRS.Adapters.MartenDbEventStore.Tests;

[Collection("PostgreSQL Integration")]
public sealed class MartenDbEventStreamSessionTests(PostgreSqlContainerFixture fixture)
{
    private readonly IEventMapper<IInventoryEvent, IInventoryEventDto> _mapper =
        new EventStoreInventoryEventMapper();

    private readonly IEventStreamProjection<Option<InventoryState>, IInventoryEvent> _projection =
        new InventoryEventStreamStateProjection();

    private MartenDbEventStoreAdapter<
        Option<InventoryState>,
        IInventoryEvent,
        IInventoryEventDto
    > CreateAdapter() => fixture.CreateAdapter();

    // -------------------------------------------------------------------------
    // GetAllEvents
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllEvents_OnNewStream_ReturnsEmptyStream()
    {
        var streamId = InventoryEventStreamId.GetStreamId(InventoryId.NewId());
        await using var session = CreateAdapter().Open(streamId, _mapper);

        var result = await session.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.Events.ShouldBeEmpty();
        result.StreamVersion.ShouldBe(EventStreamVersion.InitialVersion);
        result.StreamId.ShouldBe(streamId);
    }

    // -------------------------------------------------------------------------
    // GetNewEvents
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetNewEvents_OnFreshSession_ReturnsEmptyStream()
    {
        var streamId = InventoryEventStreamId.GetStreamId(InventoryId.NewId());
        await using var session = CreateAdapter().Open(streamId, _mapper);

        var result = session.GetNewEvents();

        result.Events.ShouldBeEmpty();
        result.StreamVersion.ShouldBe(EventStreamVersion.InitialVersion);
    }

    // -------------------------------------------------------------------------
    // AppendEvents
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AppendEvents_BuffersEventsInSessionWithoutPersisting()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-BUFFER");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        await using var session = CreateAdapter().Open(streamId, _mapper);
        session.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);

        var newEvents = session.GetNewEvents();
        newEvents.Events.Count.ShouldBe(1);

        await using var checkSession = CreateAdapter().Open(streamId, _mapper);
        var persisted = await checkSession.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );
        persisted.Events.ShouldBeEmpty();
    }

    [Fact]
    public async Task AppendEvents_AfterSave_ThrowsSessionIsLockedException()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-LOCKED");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        await using var session = CreateAdapter().Open(streamId, _mapper);
        session.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session.Save(cancellationToken: TestContext.Current.CancellationToken);

        Should.Throw<SessionIsLockedException>(() =>
            session.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null)
        );
    }

    // -------------------------------------------------------------------------
    // Save
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Save_PersistsEventsToDatabase()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-SAVE");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        await using var session = CreateAdapter().Open(streamId, _mapper);
        session.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session.Save(cancellationToken: TestContext.Current.CancellationToken);

        await using var reload = CreateAdapter().Open(streamId, _mapper);
        var result = await reload.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.Events.Count.ShouldBe(1);
        result.Events.Single().Event.ShouldBeOfType<InventoryCreated>();
    }

    [Fact]
    public async Task Save_SequentialSessions_AppendEventsCorrectly()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-SEQ");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        await using var session1 = CreateAdapter().Open(streamId, _mapper);
        session1.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);

        // State must be loaded before appending to an existing stream so the session
        // sets _storedRevision and uses Append (not StartStream) when saving.
        await using var session2 = CreateAdapter().Open(streamId, _mapper);
        await session2.GetState(
            _projection,
            cancellationToken: TestContext.Current.CancellationToken
        );
        session2.AppendEvents(
            [
                new ItemsAddedToInventory(
                    entityId,
                    entityName,
                    PositiveInteger.CreateUnsafe(10),
                    None,
                    PositiveInteger.CreateUnsafe(10)
                ),
            ],
            null,
            null
        );
        await session2.Save(cancellationToken: TestContext.Current.CancellationToken);

        await using var reload = CreateAdapter().Open(streamId, _mapper);
        var result = await reload.GetAllEvents(
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.Events.Count.ShouldBe(2);
    }

    // -------------------------------------------------------------------------
    // GetState
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetState_OnNewStream_ReturnsInitialState()
    {
        var entityId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        await using var session = CreateAdapter().Open(streamId, _mapper);
        var result = await session.GetState(
            _projection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.IsRight.ShouldBeTrue();
        var state = result.Match(Left: _ => Option<InventoryState>.None, Right: s => s);
        state.IsNone.ShouldBeTrue();
    }

    [Fact]
    public async Task GetState_AfterSave_ReturnsProjectedState()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-STATE");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        await using var session1 = CreateAdapter().Open(streamId, _mapper);
        session1.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);

        await using var session2 = CreateAdapter().Open(streamId, _mapper);
        var result = await session2.GetState(
            _projection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.IsRight.ShouldBeTrue();
        var state = result.Match(Left: _ => Option<InventoryState>.None, Right: s => s);
        state.IsSome.ShouldBeTrue();
        state.Map(s => s.IsActive).ShouldBe(Some(true));
        state.Map(s => s.Name.ToString()).ShouldBe(Some((string)entityName));
    }

    [Fact]
    public async Task GetState_WithUnsavedNewEvents_ReflectsAllEvents()
    {
        var entityId = InventoryId.NewId();
        var entityName = InventoryName.CreateUnsafe("INV-UNSAVED");
        var updatedName = InventoryName.CreateUnsafe("INV-RENAMED");
        var streamId = InventoryEventStreamId.GetStreamId(entityId);

        await using var session1 = CreateAdapter().Open(streamId, _mapper);
        session1.AppendEvents([new InventoryCreated(entityId, entityName, true)], null, null);
        await session1.Save(cancellationToken: TestContext.Current.CancellationToken);

        await using var session2 = CreateAdapter().Open(streamId, _mapper);
        session2.AppendEvents(
            [new InventoryRenamed(entityId, entityName, updatedName)],
            null,
            null
        );

        var result = await session2.GetState(
            _projection,
            cancellationToken: TestContext.Current.CancellationToken
        );

        result.IsRight.ShouldBeTrue();
        result
            .Match(Left: _ => Option<InventoryState>.None, Right: s => s)
            .Map(s => s.Name.ToString())
            .ShouldBe(Some((string)updatedName));
    }
}
