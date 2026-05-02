using System.Collections.Concurrent;
using CQRS.Ports.ProjectionStore;

namespace CQRS.Adapters.InMemoryProjectionStore;

public sealed class InMemoryProjectionStoreAdapter<TViewModel> : IProjectionStore<TViewModel>
    where TViewModel : class
{
    private readonly ConcurrentDictionary<
        DocumentCollectionId,
        IProjectionDocumentCollection<TViewModel>
    > _documentCollections = new();

    public Task<IProjectionDocumentCollection<TViewModel>> OpenDocumentCollection(
        DocumentCollectionId collectionId
    ) =>
        Task.FromResult(
            _documentCollections.GetOrAdd(
                collectionId,
                new InMemoryDocumentCollection<TViewModel>()
            )
        );

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
