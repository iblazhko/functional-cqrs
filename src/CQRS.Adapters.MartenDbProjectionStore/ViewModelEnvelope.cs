using Marten.Schema;

namespace CQRS.Adapters.MartenDbProjectionStore;

internal sealed class ViewModelEnvelope<TViewModel>
    where TViewModel : class
{
    public string Id { get; init; } = string.Empty;

    [Version]
    public Guid ETag { get; set; }

    public long Version { get; init; }

    public TViewModel? VM { get; init; }
}
