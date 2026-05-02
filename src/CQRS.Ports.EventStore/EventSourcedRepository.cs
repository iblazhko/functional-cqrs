using LanguageExt;
using static LanguageExt.Prelude;

namespace CQRS.Ports.EventStore;

public sealed class EventSourcedRepository<TDomainState, TDomainEvent, TEventDto>(
    IEventStore<TDomainState, TDomainEvent, TEventDto> eventStore
)
{
    private IEventStore<TDomainState, TDomainEvent, TEventDto> EventStore { get; } = eventStore;

    public async Task<Either<EventDeserializationError, TDomainState>> GetState(
        EventStreamId streamId,
        IEventMapper<TDomainEvent, TEventDto> eventMapper,
        IEventStreamProjection<TDomainState, TDomainEvent> stateProjection,
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    )
    {
        await using var session = EventStore.Open(streamId, eventMapper);
        return await session.GetState(stateProjection, deadline, cancellationToken);
    }

    public async Task<Either<EventDeserializationError, IEnumerable<TDomainEvent>>> AddEvents(
        EventStreamId streamId,
        IEventMapper<TDomainEvent, TEventDto> eventMapper,
        IEventStreamProjection<TDomainState, TDomainEvent> stateProjection,
        Func<TDomainState, IEnumerable<TDomainEvent>> action,
        Guid? correlationId = default,
        Guid? causationId = default,
        TimeSpan deadline = default,
        CancellationToken cancellationToken = default
    )
    {
        await using var session = EventStore.Open(streamId, eventMapper);
        var stateResult = await session.GetState(stateProjection, deadline, cancellationToken);

        return await stateResult.Match(
            Left: err => Task.FromResult(Left<EventDeserializationError, IEnumerable<TDomainEvent>>(err)),
            Right: async state =>
            {
                var newEvents = action(state).ToList();
                if (newEvents.Count > 0)
                {
                    session.AppendEvents(newEvents, correlationId, causationId);
                    await session.Save(deadline, cancellationToken);
                }
                return Right<EventDeserializationError, IEnumerable<TDomainEvent>>(newEvents);
            }
        );
    }
}
