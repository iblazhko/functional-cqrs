using CQRS.Ports.EventStore;

namespace CQRS.Adapters.InMemoryEventStore;

public sealed class InMemoryEventStoreAdapter<TDomainState, TDomainEvent, TEventDto>
    : IEventStore<TDomainState, TDomainEvent, TEventDto>
    where TEventDto : class
{
    public IEventStreamSession<TDomainState, TDomainEvent> Open(
        EventStreamId streamId,
        IEventMapper<TDomainEvent, TEventDto> eventMapper,
        IEventSerializer? eventSerializer = null
    ) =>
        new InMemoryEventStreamSession<TDomainState, TDomainEvent, TEventDto>(
            _store,
            streamId,
            eventMapper,
            eventSerializer ?? EventJsonSerializer.Instance
        );

    public Task<bool> Contains(
        EventStreamId streamId,
        CancellationToken cancellationToken = default
    ) => Task.FromResult(_store.Contains(streamId));

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private readonly InMemoryEventStoreDB _store = new();
}
