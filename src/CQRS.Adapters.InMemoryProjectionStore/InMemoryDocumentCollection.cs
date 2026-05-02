using System.Collections.Concurrent;
using CQRS.Ports.ProjectionStore;
using Serilog;

namespace CQRS.Adapters.InMemoryProjectionStore;

public sealed class InMemoryDocumentCollection<TViewModel>
    : IProjectionDocumentCollection<TViewModel>
    where TViewModel : class
{
    private readonly ConcurrentDictionary<DocumentId, TViewModel> _documents = new();

    private static TViewModel GetNewVm() => Activator.CreateInstance<TViewModel>();

    public Task<TViewModel?> GetById(DocumentId documentId)
    {
        Log.Logger.Information("[PROJECTION] Retrieving {DocumentId}", documentId);
        _documents.TryGetValue(documentId, out var result);
        return Task.FromResult(result);
    }

    public Task Update(DocumentId documentId, TViewModel vm)
    {
        Log.Logger.Information("[PROJECTION] Storing {DocumentId}", documentId);
        _documents.AddOrUpdate(documentId, vm, (_, _) => vm);
        return Task.CompletedTask;
    }

    public Task Update(DocumentId documentId, Func<TViewModel, TViewModel> getUpdatedVm)
    {
        Log.Logger.Information("[PROJECTION] Storing {DocumentId}", documentId);
        _documents.AddOrUpdate(documentId, getUpdatedVm(GetNewVm()), (_, v) => getUpdatedVm(v));
        return Task.CompletedTask;
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
