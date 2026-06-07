using CQRS.Ports.EventStore;
using JasperFx.Events;
using Marten;
using Marten.Events.Projections;

namespace CQRS.Adapters.MartenDbEventStore;

public sealed class MartenDbProjectionAdapter<TViewModel, TDomainEvent, TEventDto>(
    IEventStoreProjection<TViewModel, TDomainEvent> projection,
    IEventMapper<TDomainEvent, TEventDto> mapper,
    Func<string, string> streamKeyToDocumentId
) : IProjection
    where TViewModel : class, new()
    where TEventDto : class
{
    public async Task ApplyAsync(
        IDocumentOperations operations,
        IReadOnlyList<IEvent> events,
        CancellationToken cancellation
    )
    {
        foreach (var group in events.GroupBy(e => e.StreamKey ?? string.Empty))
        {
            var id = streamKeyToDocumentId(group.Key);
            var vm = await operations.LoadAsync<TViewModel>(id, cancellation) ?? new TViewModel();
            foreach (var e in group)
                if (e.Data is TEventDto dto)
                    vm = mapper
                        .ToDomainEvent(dto)
                        .Match(Left: _ => vm, Right: de => projection.Apply(vm, de));
            operations.Store(vm);
        }
    }
}
