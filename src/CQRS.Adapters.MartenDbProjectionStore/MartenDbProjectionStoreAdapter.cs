using CQRS.Ports.ProjectionStore;
using Marten;

namespace CQRS.Adapters.MartenDbProjectionStore;

public sealed class MartenDbProjectionStoreAdapter<TViewModel>(IDocumentStore documentStore)
    : IProjectionStore<TViewModel>
    where TViewModel : class
{
    // in the case of MartenDb, there are no explicit documents collections (like e.g. in MongoDb)
    // so MartenDbProjectionStore simply forwards the documentStore instance
    // (which is supposed to be a singleton) to MartenDbDocumentCollection
    public Task<IProjectionDocumentCollection<TViewModel>> OpenDocumentCollection(
        DocumentCollectionId collectionId
    ) =>
        Task.FromResult<IProjectionDocumentCollection<TViewModel>>(
            new MartenDbDocumentCollection<TViewModel>(documentStore)
        );

    // MartenDb IDocumentStore instance lifecycle is managed by the application host
    // hence no disposing is necessary here

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
