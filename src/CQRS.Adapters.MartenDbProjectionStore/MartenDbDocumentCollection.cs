using CQRS.Ports.ProjectionStore;
using Marten;
using Serilog;

namespace CQRS.Adapters.MartenDbProjectionStore;

public sealed class MartenDbDocumentCollection<TViewModel>(IDocumentStore documentStore)
    : IProjectionDocumentCollection<TViewModel>
    where TViewModel : class
{
    private readonly IDocumentSession _session = documentStore.LightweightSession();

    private static TViewModel GetNewVm() => Activator.CreateInstance<TViewModel>();

    private async Task<DocumentEnvelope<TViewModel>?> TryGetEnvelopeByDocumentId(
        DocumentId documentId
    )
    {
        var envelope = await _session.LoadAsync<DocumentEnvelope<TViewModel>>(documentId);

        return envelope;
    }

    public async Task<TViewModel?> GetById(DocumentId documentId)
    {
        Log.Logger.Information("[PROJECTION] Retrieving {DocumentId}", documentId);
        var envelope = await TryGetEnvelopeByDocumentId(documentId);

        return envelope?.VM;
    }

    public async Task Update(DocumentId documentId, TViewModel vm)
    {
        Log.Logger.Information("[PROJECTION] Storing {DocumentId}", documentId);

        var envelope = await TryGetEnvelopeByDocumentId(documentId);

        var newEnvelope = new DocumentEnvelope<TViewModel>
        {
            Id = documentId,
            Version = (envelope?.Version ?? DocumentVersion.New) + 1,
            VM = vm,
        };

        _session.Store(newEnvelope);
        await _session.SaveChangesAsync();
    }

    public async Task Update(DocumentId documentId, Func<TViewModel, TViewModel> getUpdatedVm)
    {
        var envelope = await TryGetEnvelopeByDocumentId(documentId);

        var newEnvelope = new DocumentEnvelope<TViewModel>
        {
            Id = documentId,
            Version = (envelope?.Version ?? DocumentVersion.New) + 1,
            VM = getUpdatedVm(envelope?.VM ?? GetNewVm()),
        };

        _session.Store(newEnvelope);
        await _session.SaveChangesAsync();
    }

    // ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
    public void Dispose()
    {
        _session?.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        _session?.Dispose();
        return ValueTask.CompletedTask;
    }
    // ReSharper restore ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
}
