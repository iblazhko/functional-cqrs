using CQRS.Ports.EventStore;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CQRS.Adapters.InMemoryEventStore;

// ReSharper disable UnusedType.Global
// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable

public sealed class InMemoryEventStreamSession<TDomainState, TDomainEvent, TEventDto>
    : IEventStreamSession<TDomainState, TDomainEvent>
{
    private readonly InMemoryEventStoreDB _eventStore;
    private readonly EventStreamId _streamId;
    private readonly IEventMapper<TDomainEvent, TEventDto> _eventMapper;
    private readonly IEventSerializer _eventSerializer;
    private readonly IEventTypeResolver _eventTypeResolver;

    private readonly Either<EventDeserializationError, EventStream<TDomainEvent>> _eventStream;
    private readonly List<EventWithMetadata<TDomainEvent>> _newEvents;
    private bool _isLocked;

    public InMemoryEventStreamSession(
        InMemoryEventStoreDB eventStore,
        EventStreamId streamId,
        IEventMapper<TDomainEvent, TEventDto> eventMapper,
        IEventSerializer eventSerializer
    )
    {
        _eventStore = eventStore;
        _streamId = streamId;
        _eventMapper = eventMapper;
        _eventSerializer = eventSerializer;
        _eventTypeResolver = new EventTypeResolver<TEventDto>();

        var serializedEventStream = _eventStore.Open(streamId);
        _eventStream = FromSerializedEventsStream(serializedEventStream);
        _newEvents = [];
    }

    public Task<Either<EventDeserializationError, TDomainState>> GetState(
        IEventStreamProjection<TDomainState, TDomainEvent> projection,
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(
            _eventStream.Map(stream =>
                stream
                    .Events.Concat(_newEvents)
                    .Aggregate(
                        projection.GetInitialState(_streamId),
                        (state, em) => projection.Apply(state, em.Event)
                    )
            )
        );

    public Task<EventStream<TDomainEvent>> GetAllEvents(
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    )
    {
        AssertSessionIsNotLocked();

        return Task.FromResult(
            new EventStream<TDomainEvent>(
                _streamId,
                GetUpdatedStreamVersion(),
                GetAllEventsWithMetadata()
            )
        );
    }

    private List<EventWithMetadata<TDomainEvent>> GetAllEventsWithMetadata() =>
        _eventStream.Match(
            Left: err =>
                throw new InvalidOperationException(
                    $"Event deserialization failed for '{err.EventType}': {err.Reason}"
                ),
            Right: stream => stream.Events.Concat(_newEvents).ToList()
        );

    public EventStream<TDomainEvent> GetNewEvents() =>
        new(_streamId, GetUpdatedStreamVersion(), _newEvents);

    public void AppendEvents(
        IEnumerable<TDomainEvent> events,
        Guid? correlationId,
        Guid? causationId
    )
    {
        _newEvents.AddRange(
            events.Select(x =>
                EventWithMetadata<TDomainEvent>.FromEvent(
                    x,
                    correlationId ?? Guid.Empty,
                    causationId ?? Guid.Empty,
                    DateTime.UtcNow
                )
            )
        );
    }

    public void AppendEvents(IEnumerable<EventWithMetadata<TDomainEvent>> events)
    {
        _newEvents.AddRange(events);
    }

    public Task Save(TimeSpan deadline = default, CancellationToken cancellationToken = default)
    {
        _eventStore.AppendEvents(_streamId, GetNewSerializedEvents(), GetStoredStreamVersion());
        _isLocked = true;
        return Task.CompletedTask;
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private void AssertSessionIsNotLocked()
    {
        if (_isLocked)
            throw new SessionIsLockedException(_streamId);
    }

    private EventStreamVersion GetStoredStreamVersion() =>
        _eventStream.Match(
            Left: _ => EventStreamVersion.InitialVersion,
            Right: stream => stream.StreamVersion
        );

    private EventStreamVersion GetUpdatedStreamVersion() =>
        (EventStreamVersion)((long)GetStoredStreamVersion() + _newEvents.Count);

    private Either<EventDeserializationError, EventStream<TDomainEvent>> FromSerializedEventsStream(
        SerializedEventStream serializedEventStream
    ) =>
        serializedEventStream
            .Events.Aggregate(
                Right<EventDeserializationError, List<EventWithMetadata<TDomainEvent>>>(
                    new List<EventWithMetadata<TDomainEvent>>()
                ),
                (acc, x) =>
                    acc.Bind(list =>
                        _eventMapper
                            .ToDomainEvent(
                                (TEventDto)
                                    _eventSerializer.Deserialize(
                                        x.Data,
                                        _eventTypeResolver.GetEventType(x.EventTypeFullName)
                                    )
                            )
                            .Map(evt =>
                            {
                                list.Add(
                                    EventWithMetadata<TDomainEvent>.FromEvent(
                                        evt,
                                        Guid.Empty,
                                        Guid.Empty,
                                        DateTime.UtcNow
                                    )
                                );
                                return list;
                            })
                    )
            )
            .Map(events => new EventStream<TDomainEvent>(
                _streamId,
                (EventStreamVersion)serializedEventStream.Events.Length,
                events.ToArray()
            ));

    private SerializedEvent[] GetNewSerializedEvents() =>
        _newEvents
            .Select(x =>
            {
                var eventDto = _eventMapper.ToEventDto(x.Event);
                if (eventDto is null)
                {
                    throw new InvalidOperationException();
                }

                return new SerializedEvent(
                    eventDto.GetType().FullName!,
                    _eventSerializer.Serialize(eventDto)
                );
            })
            .ToArray();
}
