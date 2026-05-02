using CQRS.Adapters.InMemoryEventStore;
using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Application.Inventory;
using CQRS.Application.WolverineHandlers;
using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.EntityIds;
using CQRS.Mapping.Inventory.V1;
using CQRS.Ports.EventStore;
using Shouldly;
using Wolverine;

namespace CQRS.Application.WolverineHandlers.Tests;

public sealed class InventoryCommandConsumerTests
{
    private sealed class SpyStatusRecorder : ICommandProcessingStatusRecordingService
    {
        public CommandProcessingRequest? StartedRequest { get; private set; }
        public Guid? CompletedCommandId { get; private set; }
        public Guid? RejectedCommandId { get; private set; }
        public Guid? FailedCommandId { get; private set; }

        public void Reset()
        {
            StartedRequest = null;
            CompletedCommandId = null;
            RejectedCommandId = null;
            FailedCommandId = null;
        }

        public Task RecordCommandProcessingStarted(CommandProcessingRequest request)
        {
            StartedRequest = request;
            return Task.CompletedTask;
        }

        public Task RecordCommandProcessingCompleted(Guid commandId, DateTimeOffset completedAt, string response = "")
        {
            CompletedCommandId = commandId;
            return Task.CompletedTask;
        }

        public Task RecordCommandProcessingRejected(Guid commandId, DateTimeOffset rejectedAt, string reason)
        {
            RejectedCommandId = commandId;
            return Task.CompletedTask;
        }

        public Task RecordCommandProcessingFailed(Guid commandId, DateTimeOffset failedAt, string failure)
        {
            FailedCommandId = commandId;
            return Task.CompletedTask;
        }
    }

    private readonly SpyStatusRecorder _recorder = new();
    private readonly InMemoryEventStoreAdapter<InventoryState, IInventoryEvent, IInventoryEventDto> _eventStore = new();
    private readonly InventoryCommandConsumer _consumer;

    public InventoryCommandConsumerTests()
    {
        _consumer = MakeConsumer(_eventStore);
    }

    private InventoryCommandConsumer MakeConsumer(
        IEventStore<InventoryState, IInventoryEvent, IInventoryEventDto> store
    ) => new(
        new TimeProviderStub(),
        new MoonPhaseServiceStub(MoonPhase.NewMoon),
        _recorder,
        new InventoryCommandDtoHandler(
            store,
            new InventoryCommandV1Mapper(),
            new InventoryEventStreamStateProjection(),
            new EventStoreInventoryEventMapper()
        )
    );

    private static string ValidId() => (string)EntityId.NewId();

    private static Envelope MakeEnvelope(
        Guid? id = null,
        string? correlationId = null,
        string? parentId = null
    ) => new() { Id = id ?? Guid.NewGuid(), CorrelationId = correlationId, ParentId = parentId };

    // --- Context building ---

    [Fact]
    public async Task Consume_SetsCommandIdFromEnvelopeId()
    {
        var envelopeId = Guid.NewGuid();
        await _consumer.Consume(new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" }, MakeEnvelope(id: envelopeId));
        _recorder.StartedRequest!.CommandId.ShouldBe(envelopeId);
    }

    [Fact]
    public async Task Consume_WhenCorrelationIdIsValidGuid_UsesItAsCorrelationId()
    {
        var corrId = Guid.NewGuid();
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope(correlationId: corrId.ToString())
        );
        _recorder.StartedRequest!.CorrelationId.ShouldBe(corrId);
    }

    [Fact]
    public async Task Consume_WhenCorrelationIdIsNull_FallsBackToEnvelopeId()
    {
        var envelopeId = Guid.NewGuid();
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope(id: envelopeId, correlationId: null)
        );
        _recorder.StartedRequest!.CorrelationId.ShouldBe(envelopeId);
    }

    [Fact]
    public async Task Consume_WhenCorrelationIdIsNotAGuid_FallsBackToEnvelopeId()
    {
        var envelopeId = Guid.NewGuid();
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope(id: envelopeId, correlationId: "not-a-guid")
        );
        _recorder.StartedRequest!.CorrelationId.ShouldBe(envelopeId);
    }

    [Fact]
    public async Task Consume_WhenParentIdIsValidGuid_SetsCausationId()
    {
        var parentId = Guid.NewGuid();
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope(parentId: parentId.ToString())
        );
        _recorder.StartedRequest!.CausationId.ShouldBe(parentId);
    }

    [Fact]
    public async Task Consume_WhenParentIdIsNull_CausationIdIsEmpty()
    {
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope(parentId: null)
        );
        _recorder.StartedRequest!.CausationId.ShouldBe(Guid.Empty);
    }

    // --- Status recording lifecycle ---

    [Fact]
    public async Task Consume_AlwaysRecordsProcessingStarted()
    {
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope()
        );
        _recorder.StartedRequest.ShouldNotBeNull();
    }

    [Fact]
    public async Task Consume_WhenCommandSucceeds_RecordsCompletedWithMatchingCommandId()
    {
        var envelopeId = Guid.NewGuid();
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope(id: envelopeId)
        );
        _recorder.CompletedCommandId.ShouldBe(envelopeId);
        _recorder.RejectedCommandId.ShouldBeNull();
        _recorder.FailedCommandId.ShouldBeNull();
    }

    [Fact]
    public async Task Consume_WhenDtoMappingFails_RecordsRejectedWithMatchingCommandId()
    {
        var envelopeId = Guid.NewGuid();
        // Invalid InventoryId format → EntityId.Create fails → mapping fault → Rejected
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = "invalid-id-format", Name = "Widget" },
            MakeEnvelope(id: envelopeId)
        );
        _recorder.RejectedCommandId.ShouldBe(envelopeId);
        _recorder.CompletedCommandId.ShouldBeNull();
        _recorder.FailedCommandId.ShouldBeNull();
    }

    [Fact]
    public async Task Consume_WhenDomainLogicFails_RecordsFailed()
    {
        var id = ValidId();
        var createCmd = new CreateInventoryCommand { InventoryId = id, Name = "Widget" };

        // First create succeeds
        await _consumer.Consume(createCmd, MakeEnvelope());
        _recorder.Reset();

        // Second create on the same ID triggers InventoryAlreadyExists domain error → Failed
        var envelopeId = Guid.NewGuid();
        await _consumer.Consume(createCmd, MakeEnvelope(id: envelopeId));

        _recorder.FailedCommandId.ShouldBe(envelopeId);
        _recorder.CompletedCommandId.ShouldBeNull();
        _recorder.RejectedCommandId.ShouldBeNull();
    }

    // --- Exception path ---

    [Fact]
    public async Task Consume_WhenEventStoreThrows_RecordsFailedAndRethrows()
    {
        var consumer = MakeConsumer(new ThrowingEventStore());
        var envelopeId = Guid.NewGuid();

        await Should.ThrowAsync<InvalidOperationException>(
            () => consumer.Consume(
                new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
                MakeEnvelope(id: envelopeId)
            )
        );

        _recorder.StartedRequest.ShouldNotBeNull();
        _recorder.FailedCommandId.ShouldBe(envelopeId);
    }

    // --- All five Consume overloads dispatch through the same pipeline ---

    [Fact]
    public async Task Consume_CreateInventoryCommand_RecordsStarted()
    {
        await _consumer.Consume(
            new CreateInventoryCommand { InventoryId = ValidId(), Name = "Widget" },
            MakeEnvelope()
        );
        _recorder.StartedRequest.ShouldNotBeNull();
    }

    [Fact]
    public async Task Consume_RenameInventoryCommand_RecordsStarted()
    {
        // Invalid ID → Rejected, but Started is always recorded first
        await _consumer.Consume(
            new RenameInventoryCommand { InventoryId = "invalid-id", NewName = "Gadget" },
            MakeEnvelope()
        );
        _recorder.StartedRequest.ShouldNotBeNull();
    }

    [Fact]
    public async Task Consume_AddItemsToInventoryCommand_RecordsStarted()
    {
        await _consumer.Consume(
            new AddItemsToInventoryCommand { InventoryId = "invalid-id", Count = 5 },
            MakeEnvelope()
        );
        _recorder.StartedRequest.ShouldNotBeNull();
    }

    [Fact]
    public async Task Consume_RemoveItemsFromInventoryCommand_RecordsStarted()
    {
        await _consumer.Consume(
            new RemoveItemsFromInventoryCommand { InventoryId = "invalid-id", Count = 3 },
            MakeEnvelope()
        );
        _recorder.StartedRequest.ShouldNotBeNull();
    }

    [Fact]
    public async Task Consume_DeactivateInventoryCommand_RecordsStarted()
    {
        await _consumer.Consume(
            new DeactivateInventoryCommand { InventoryId = "invalid-id" },
            MakeEnvelope()
        );
        _recorder.StartedRequest.ShouldNotBeNull();
    }
}

file sealed class TimeProviderStub : ITimeProvider
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    public DateTimeOffset GetUtcNow() => Timestamp;
    public Domain.TimeZone TimeZone { get; } = Domain.TimeZone.CreateUnsafe("Europe/London");
}

file sealed class MoonPhaseServiceStub(MoonPhase phase) : IMoonPhaseService
{
    public Task<MoonPhase> GetMoonPhase(Domain.TimeZone timeZone, DateTimeOffset time) =>
        Task.FromResult(phase);
}

file sealed class ThrowingEventStore : IEventStore<InventoryState, IInventoryEvent, IInventoryEventDto>
{
    public IEventStreamSession<InventoryState, IInventoryEvent> Open(
        EventStreamId streamId,
        IEventMapper<IInventoryEvent, IInventoryEventDto> eventMapper,
        IEventSerializer? eventSerializer = null
    ) => throw new InvalidOperationException("Simulated event store failure");

    public Task<bool> Contains(EventStreamId streamId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
