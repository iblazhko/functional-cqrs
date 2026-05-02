using System.Collections.Concurrent;
using CQRS.Ports.EventStore;

namespace CQRS.Adapters.InMemoryEventStore;

// ReSharper disable once InconsistentNaming
public sealed class InMemoryEventStoreDB
{
    public SerializedEventStream Open(string streamName) =>
        _eventStreams.GetOrAdd(streamName, _ => SerializedEventStream.Empty);

    public void AppendEvents(string streamName, SerializedEvent[] events, long expectedVersion)
    {
        var currentStream = Open(streamName);
        if (currentStream.Version != expectedVersion)
            throw new ConcurrencyException(
                streamName,
                new InvalidOperationException(
                    $"Expected stream version {expectedVersion}, but got {currentStream.Version}"
                )
            );

        var newStream = new SerializedEventStream(
            currentStream.Events.Concat(events).ToArray(),
            currentStream.Version + events.Length
        );

        _eventStreams.AddOrUpdate(streamName, _ => newStream, (_, _) => newStream);
    }

    public bool Contains(string streamName) => _eventStreams.ContainsKey(streamName);

    public bool Delete(string streamName) => _eventStreams.TryRemove(streamName, out _);

    private readonly ConcurrentDictionary<string, SerializedEventStream> _eventStreams = new();
}

public sealed record SerializedEventStream(SerializedEvent[] Events, long Version)
{
    public static SerializedEventStream Empty => new([], 0);
}

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once NotAccessedPositionalProperty.Global
public sealed record SerializedEvent(string EventTypeFullName, byte[] Data);
