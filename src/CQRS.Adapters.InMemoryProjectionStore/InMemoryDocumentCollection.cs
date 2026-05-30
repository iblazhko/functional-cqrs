using System.Collections.Concurrent;
using CQRS.Ports.ProjectionStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CQRS.Adapters.InMemoryProjectionStore;

public sealed class InMemoryDocumentCollection<TViewModel>
    : IProjectionDocumentCollection<TViewModel>
    where TViewModel : class
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<DocumentId, TViewModel> _documents = new();

    public InMemoryDocumentCollection(
        ILogger<InMemoryDocumentCollection<TViewModel>>? logger = null
    )
    {
        _logger = logger ?? NullLogger<InMemoryDocumentCollection<TViewModel>>.Instance;
    }

    private static TViewModel GetNewVm() => Activator.CreateInstance<TViewModel>();

    public Task<TViewModel?> GetById(DocumentId documentId)
    {
        _logger.LogInformation("[PROJECTION] Retrieving {DocumentId}", documentId);
        _documents.TryGetValue(documentId, out var result);
        return Task.FromResult(result);
    }

    public Task Update(DocumentId documentId, TViewModel vm)
    {
        _logger.LogInformation("[PROJECTION] Storing {DocumentId}", documentId);
        _documents.AddOrUpdate(documentId, vm, (_, _) => vm);
        return Task.CompletedTask;
    }

    public Task Update(DocumentId documentId, Func<TViewModel, TViewModel> getUpdatedVm)
    {
        _logger.LogInformation("[PROJECTION] Storing {DocumentId}", documentId);
        _documents.AddOrUpdate(documentId, getUpdatedVm(GetNewVm()), (_, v) => getUpdatedVm(v));
        return Task.CompletedTask;
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
