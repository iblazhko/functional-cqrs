using CQRS.Ports.ProjectionStore;
using Marten;
using Marten.Exceptions;
using Microsoft.Extensions.Logging;
using ProjectionConcurrencyException = CQRS.Ports.ProjectionStore.ConcurrencyException;

namespace CQRS.Adapters.MartenDbProjectionStore;

public sealed class MartenDbDocumentCollection<TViewModel>(
    IDocumentStore documentStore,
    ILogger<MartenDbDocumentCollection<TViewModel>> logger
) : IProjectionDocumentCollection<TViewModel>
    where TViewModel : class
{
    private const int MaxAttempts = 3;
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(150),
    ];

    private static TViewModel GetNewVm() => Activator.CreateInstance<TViewModel>();

    public async Task<TViewModel?> GetById(DocumentId documentId)
    {
        logger.LogInformation("[PROJECTION] Retrieving {DocumentId}", documentId);
        using var session = documentStore.LightweightSession();
        var envelope = await session.LoadAsync<ViewModelEnvelope<TViewModel>>(documentId);
        return envelope?.VM;
    }

    public Task Update(DocumentId documentId, TViewModel vm) => Update(documentId, _ => vm);

    public async Task Update(DocumentId documentId, Func<TViewModel, TViewModel> getUpdatedVm)
    {
        logger.LogInformation("[PROJECTION] Storing {DocumentId}", documentId);
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(RetryDelays[attempt]);

            if (await TryUpdate(documentId, getUpdatedVm))
                return;
        }

        throw new ProjectionConcurrencyException();
    }

    private async Task<bool> TryUpdate(
        DocumentId documentId,
        Func<TViewModel, TViewModel> getUpdatedVm
    )
    {
        using var session = documentStore.LightweightSession();
        var envelope = await session.LoadAsync<ViewModelEnvelope<TViewModel>>(documentId);

        var newEnvelope = new ViewModelEnvelope<TViewModel>
        {
            Id = documentId,
            Version = (envelope?.Version ?? DocumentVersion.New) + 1,
            VM = getUpdatedVm(envelope?.VM ?? GetNewVm()),
        };

        try
        {
            if (envelope is null)
                session.Insert(newEnvelope);
            else
                session.UpdateExpectedVersion(newEnvelope, envelope.ETag);

            await session.SaveChangesAsync();
            return true;
        }
        catch (JasperFx.ConcurrencyException)
        {
            return false;
        }
        catch (MartenCommandException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

    private static bool IsUniqueViolation(MartenCommandException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: "23505" };

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
