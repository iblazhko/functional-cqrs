using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using LanguageExt;

namespace CQRS.Ports.EventStore;

public sealed record EventDeserializationError(string EventType, string Reason);

// ReSharper disable NotAccessedPositionalProperty.Global

public readonly record struct EventStreamId
{
    private string Value { get; }

    private EventStreamId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));
        Value = value;
    }

    public static implicit operator string(EventStreamId id) => id.Value;

    public static explicit operator EventStreamId(string id) => new(id);

    public override string ToString() => Value;
}

public readonly record struct EventStreamVersion
{
    private long Value { get; }

    private EventStreamVersion(long value)
    {
        Value = value;
    }

    public static EventStreamVersion InitialVersion => new(0);

    public static implicit operator long(EventStreamVersion id) => id.Value;

    public static explicit operator EventStreamVersion(long id) => new(id);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

public sealed record EventMetadata(
    string EventTypeFullName,
    Guid EventId,
    Guid CorrelationId,
    Guid? CausationId,
    DateTime Timestamp
)
{
    public static EventMetadata NewEventMetadata(string eventTypeFullName, DateTime timestamp) =>
        new(eventTypeFullName, Guid.NewGuid(), Guid.NewGuid(), default, timestamp);
}

public sealed record EventWithMetadata<T>(T Event, EventMetadata Metadata)
{
    public static EventWithMetadata<T> FromEvent(
        T evt,
        Guid correlationId,
        Guid causationId,
        DateTime timestamp
    ) =>
        new(
            evt,
            new EventMetadata(
                evt?.GetType().FullName ?? "N/A",
                Guid.NewGuid(),
                correlationId,
                causationId,
                timestamp
            )
        );
}

public sealed record EventStream<TDomainEvent>(
    EventStreamId StreamId,
    EventStreamVersion StreamVersion,
    IReadOnlyCollection<EventWithMetadata<TDomainEvent>> Events
);

public interface IEventTypeResolver
{
    string GetEventTypeName(object evt) =>
        evt.GetType().Name ?? throw new ArgumentNullException(nameof(evt));
    string GetEventTypeFullName(object evt) =>
        evt.GetType().FullName ?? throw new ArgumentNullException(nameof(evt));
    Type GetEventType(string eventTypeName);
}

public sealed class EventTypeResolver<T> : IEventTypeResolver
{
    public Type GetEventType(string eventTypeName) =>
        _eventTypeByName.GetOrAdd(
            eventTypeName,
            x =>
                typeof(T).Assembly.GetType(x)
                ?? throw new InvalidOperationException($"Could not resolve event type '{x}'")
        );

    private readonly ConcurrentDictionary<string, Type> _eventTypeByName = new();
}

public interface IEventSerializer
{
    byte[] Serialize(object instance);
    object Deserialize(ReadOnlySpan<byte> data, Type instanceType);
}

public sealed class EventJsonSerializer : IEventSerializer
{
    public byte[] Serialize(object instance) => JsonSerializer.SerializeToUtf8Bytes(instance);

    public object Deserialize(ReadOnlySpan<byte> data, Type instanceType) =>
        JsonSerializer.Deserialize(data, instanceType)
        ?? throw new InvalidOperationException(
            $"Could not deserialize event type '{instanceType.FullName}'"
        );

    public static IEventSerializer Instance => _instance.Value;

    // ReSharper disable once InconsistentNaming
    private static readonly Lazy<IEventSerializer> _instance = new(() => new EventJsonSerializer());
}

public interface IEventMapper<TDomainEvent, TEventDto>
{
    Either<EventDeserializationError, TDomainEvent> ToDomainEvent(TEventDto dto);
    TEventDto ToEventDto(TDomainEvent domain);
}

public interface IEventPublisher<TDomainEvent>
{
    Task Publish(
        IEnumerable<EventWithMetadata<TDomainEvent>> events,
        CancellationToken cancellationToken = default
    );
}

public sealed class NoOpEventPublisher<TDomainEvent> : IEventPublisher<TDomainEvent>
{
    public Task Publish(
        IEnumerable<EventWithMetadata<TDomainEvent>> events,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;
}

public interface IEventStreamProjection<TDomainState, in TDomainEvent>
{
    TDomainState GetInitialState(EventStreamId streamId);
    TDomainState Apply(TDomainState state, TDomainEvent evt);
}

public interface IEventStreamSession<TDomainState, TDomainEvent> : IDisposable, IAsyncDisposable
{
    Task<EventStream<TDomainEvent>> GetAllEvents(
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    );

    EventStream<TDomainEvent> GetNewEvents();

    Task<Either<EventDeserializationError, TDomainState>> GetState(
        IEventStreamProjection<TDomainState, TDomainEvent> projection,
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    );

    void AppendEvents(IEnumerable<TDomainEvent> events, Guid? correlationId, Guid? causationId);

    void AppendEvents(IEnumerable<EventWithMetadata<TDomainEvent>> events);

    Task Save(TimeSpan deadline = default, CancellationToken cancellationToken = default);
}

public interface IEventStore<TDomainState, TDomainEvent, TEventDto> : IDisposable, IAsyncDisposable
{
    IEventStreamSession<TDomainState, TDomainEvent> Open(
        EventStreamId streamId,
        IEventMapper<TDomainEvent, TEventDto> eventMapper,
        IEventSerializer? eventSerializer = null
    );
    Task<bool> Contains(EventStreamId streamId, CancellationToken cancellationToken = default);
}

public interface IEventStoreProjection<TViewModel, TDomainEvent>
    where TViewModel : class, new()
{
    TViewModel Apply(TViewModel current, TDomainEvent @event);
}

public interface IEventStoreProjectionReader<TViewModel>
    where TViewModel : class
{
    Task<TViewModel?> GetById(string id);
}

public sealed class UnknownEventTypeException(string typeName)
    : Exception($"Unknown event type {typeName}");

public sealed class SessionIsLockedException(string streamId)
    : Exception($"Session is locked for modifications for stream {streamId}");

public sealed class ConcurrencyException(string streamId, Exception innerException)
    : Exception($"Concurrency exception while saving stream {streamId}", innerException);
