using CQRS.Ports.EventStore;
using Marten;

namespace CQRS.Adapters.MartenDbEventStore;

public sealed class MartenDbProjectionReader<TViewModel>(IDocumentStore store)
    : IEventStoreProjectionReader<TViewModel>
    where TViewModel : class
{
    public async Task<TViewModel?> GetById(string id)
    {
        using var session = store.LightweightSession();
        return await session.LoadAsync<TViewModel>(id);
    }
}
