using CQRS.Ports.EventStore;
using LanguageExt;
using Marten;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace CQRS.Adapters.MartenDbEventStore;

internal sealed class MartenDbEventStreamSession<TDomainState, TDomainEvent, TEventDto>(
    EventStreamId streamId,
    IDocumentStore documentStore,
    IEventMapper<TDomainEvent, TEventDto> eventMapper,
    IEventPublisher<TDomainEvent> eventPublisher,
    TimeProvider timeProvider,
    ILogger<MartenDbEventStreamSession<TDomainState, TDomainEvent, TEventDto>> logger
) : IEventStreamSession<TDomainState, TDomainEvent>
    where TEventDto : class
{
    private IEventMapper<TDomainEvent, TEventDto> EventMapper { get; } = eventMapper;
    private IEventPublisher<TDomainEvent> EventPublisher { get; } = eventPublisher;
    private IDocumentSession Session { get; } = documentStore.LightweightSession();
    private TimeProvider EventTimeProvider { get; } = timeProvider;
    private EventStreamId StreamId { get; } = streamId;
    private readonly List<EventWithMetadata<TDomainEvent>> _storedEvents = new();
    private readonly List<EventWithMetadata<TDomainEvent>> _newEvents = new();
    private EventStreamVersion _storedRevision = EventStreamVersion.InitialVersion;
    private EventDeserializationError? _deserializationError;

    private IEnumerable<EventWithMetadata<TDomainEvent>> AllEvents =>
        _storedEvents.Concat(_newEvents);
    private EventStreamVersion Revision => (EventStreamVersion)(_storedRevision + _newEvents.Count);

    private bool _isLocked;

    private Either<
        EventDeserializationError,
        IReadOnlyList<EventWithMetadata<TDomainEvent>>
    > MapFromMartenEvents(IReadOnlyList<JasperFx.Events.IEvent> mtEvents) =>
        mtEvents
            .Aggregate(
                Right<EventDeserializationError, List<EventWithMetadata<TDomainEvent>>>(
                    new List<EventWithMetadata<TDomainEvent>>()
                ),
                (acc, e) =>
                    acc.Bind(list =>
                        EventMapper
                            .ToDomainEvent((TEventDto)e.Data)
                            .Map(evt =>
                            {
                                list.Add(
                                    new EventWithMetadata<TDomainEvent>(
                                        evt,
                                        EventMetadata.NewEventMetadata(
                                            e.EventType.FullName
                                                ?? throw new InvalidOperationException(
                                                    "Could not get event type full name"
                                                ),
                                            EventTimeProvider.GetUtcNow().UtcDateTime
                                        )
                                    )
                                );
                                return list;
                            })
                    )
            )
            .Map(list => (IReadOnlyList<EventWithMetadata<TDomainEvent>>)list);

    private void AssertSessionIsNotLocked()
    {
        if (_isLocked)
            throw new SessionIsLockedException(StreamId);
    }

    private async Task ReadStoredEvents(CancellationToken cancellationToken)
    {
        logger.LogDebug("[EVENTSTORE] Open event stream {EventStreamId}", StreamId);

        var mtEvents = await Session.Events.FetchStreamAsync(StreamId, token: cancellationToken);

        // ReSharper disable once ConstantConditionalAccessQualifier
        if (mtEvents.Count > 0)
        {
            MapFromMartenEvents(mtEvents)
                .Match(
                    Left: err => _deserializationError = err,
                    Right: events =>
                    {
                        _storedEvents.AddRange(events);
                        _storedRevision = (EventStreamVersion)mtEvents[^1].Version;
                    }
                );
        }
    }

    public async Task<EventStream<TDomainEvent>> GetAllEvents(
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    )
    {
        if (_storedEvents.Count == 0)
            await ReadStoredEvents(cancellationToken);
        return new EventStream<TDomainEvent>(StreamId, Revision, AllEvents.ToList());
    }

    public EventStream<TDomainEvent> GetNewEvents() => new(StreamId, Revision, _newEvents);

    public async Task<Either<EventDeserializationError, TDomainState>> GetState(
        IEventStreamProjection<TDomainState, TDomainEvent> projection,
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    )
    {
        if (_storedEvents.Count == 0)
            await ReadStoredEvents(cancellationToken);

        if (_deserializationError != null)
            return Left<EventDeserializationError, TDomainState>(_deserializationError);

        var seed = projection.GetInitialState(StreamId);
        return AllEvents.Aggregate(seed, (s, e) => projection.Apply(s, e.Event));
    }

    public void AppendEvents(
        IEnumerable<TDomainEvent> events,
        Guid? correlationId = default,
        Guid? causationId = default
    ) =>
        AppendEvents(
            events.Select(e => new EventWithMetadata<TDomainEvent>(
                e,
                new EventMetadata(
                    e?.GetType().FullName
                        ?? throw new InvalidOperationException(
                            "Could not get event type full name"
                        ),
                    Guid.NewGuid(),
                    correlationId ?? Guid.NewGuid(),
                    causationId,
                    EventTimeProvider.GetUtcNow().UtcDateTime
                )
            ))
        );

    public void AppendEvents(IEnumerable<EventWithMetadata<TDomainEvent>> events)
    {
        AssertSessionIsNotLocked();
        _newEvents.AddRange(events.Where(e => e is { Event: not null }));
    }

    public async Task Save(
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("[EVENTSTORE] Save event stream {EventStreamId}", StreamId);
        if (_newEvents.Count == 0)
            return;

        var mtEvents = _newEvents.Select(e => EventMapper.ToEventDto(e.Event)).ToList();
        _ = (long)_storedRevision switch
        {
            0 => Session.Events.StartStream(StreamId, mtEvents),
            _ => Session.Events.Append(StreamId, mtEvents),
        };

        try
        {
            await Session.SaveChangesAsync(cancellationToken);
            Lock();

            await EventPublisher.Publish(_newEvents, cancellationToken);
        }
        catch (JasperFx.Events.EventStreamUnexpectedMaxEventIdException e)
        {
            throw new ConcurrencyException(StreamId, e);
        }
    }

    private void Lock()
    {
        _isLocked = true;
    }

    public void Dispose()
    {
        Session.Dispose();
    }

    public ValueTask DisposeAsync() => Session.DisposeAsync();
}
