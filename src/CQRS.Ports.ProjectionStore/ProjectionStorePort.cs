using System.Globalization;

namespace CQRS.Ports.ProjectionStore;

public readonly record struct DocumentCollectionId
{
    private DocumentCollectionId(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id.ToUpperInvariant();
    }

    private string Id { get; }

    public static implicit operator string(DocumentCollectionId id) => id.Id;

    public static explicit operator DocumentCollectionId(string id) => new(id);

    public override string ToString() => Id;

    public static DocumentCollectionId NewId() => new(Guid.NewGuid().ToString("N"));
}

public readonly record struct DocumentId
{
    private DocumentId(string id)
    {
        Id = id.ToUpperInvariant();
    }

    private static DocumentId Create(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return new(id);
    }

    private string Id { get; }

    public static implicit operator string(DocumentId id) => id.Id;

    public static explicit operator DocumentId(string id) => Create(id);

    public override string ToString() => Id;

    public static DocumentId NewId() => Create(Guid.NewGuid().ToString("N"));

    public static DocumentId Empty { get; } = new(string.Empty);
}

public readonly record struct DocumentVersion
{
    private DocumentVersion(long version)
    {
        Version = version;
    }

    private long Version { get; }

    public static implicit operator long(DocumentVersion dv) => dv.Version;

    public static explicit operator DocumentVersion(long v) => new(v);

    public override string ToString() => Version.ToString(CultureInfo.InvariantCulture);

    public static DocumentVersion New { get; } = new(0);
}

public static class DocumentVersionExtensions
{
    public static DocumentVersion Increment(this DocumentVersion dv) => (DocumentVersion)(dv + 1);
}

public class InvalidDocumentCollectionIdException : Exception;

public class InvalidDocumentIdException : Exception;

public interface IProjectionDocumentCollection<TViewModel> : IDisposable, IAsyncDisposable
    where TViewModel : class
{
    Task<TViewModel?> GetById(DocumentId documentId);
    Task Update(DocumentId documentId, TViewModel vm);
    Task Update(DocumentId documentId, Func<TViewModel, TViewModel> getUpdatedVm);
}

public interface IProjectionStore<TViewModel> : IDisposable, IAsyncDisposable
    where TViewModel : class
{
    Task<IProjectionDocumentCollection<TViewModel>> OpenDocumentCollection(
        DocumentCollectionId collectionId
    );
}

public class ConcurrencyException : Exception;
