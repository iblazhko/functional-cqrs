using CQRS.Ports.EventStore;
using Marten;

namespace CQRS.Adapters.MartenDbEventStore;

public sealed class MartenDbEventStoreAdapter<TDomainState, TDomainEvent, TEventDto>(
    IDocumentStore documentStore,
    IEventPublisher<TDomainEvent> eventPublisher,
    TimeProvider timeProvider
) : IEventStore<TDomainState, TDomainEvent, TEventDto>
    where TEventDto : class
{
    private IDocumentStore DocumentStore { get; } = documentStore;
    private IEventPublisher<TDomainEvent> EventPublisher { get; } = eventPublisher;
    private TimeProvider EventTimeProvider { get; } = timeProvider;

    public IEventStreamSession<TDomainState, TDomainEvent> Open(
        EventStreamId streamId,
        IEventMapper<TDomainEvent, TEventDto> eventMapper,
        IEventSerializer? eventSerializer = null
    ) =>
        new MartenDbEventStreamSession<TDomainState, TDomainEvent, TEventDto>(
            streamId,
            DocumentStore,
            eventMapper,
            EventPublisher,
            EventTimeProvider
        );

    public Task Delete(EventStreamId streamId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Contains(
        EventStreamId streamId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotImplementedException();
    }

    // MartenDb IDocumentStore instance lifecycle is managed by the application host
    // hence no disposing is necessary in this adapter
    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
