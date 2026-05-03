using CQRS.Ports.ProjectionStore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace CQRS.Adapters.MartenDbProjectionStore.Tests;

[Collection("PostgreSQL Integration")]
public sealed class MartenDbProjectionStoreAdapterTests(PostgreSqlContainerFixture fixture)
{
    private MartenDbProjectionStoreAdapter<TestViewModel> CreateStore() =>
        new(fixture.DocumentStore, NullLoggerFactory.Instance);

    [Fact]
    public async Task OpenDocumentCollection_ReturnsNonNullCollection()
    {
        var store = CreateStore();

        var collection = await store.OpenDocumentCollection(DocumentCollectionId.NewId());

        collection.ShouldNotBeNull();
    }

    [Fact]
    public async Task OpenDocumentCollection_DocumentStoredViaOneOpening_IsRetrievableViaAnother()
    {
        var store = CreateStore();
        var collectionId = DocumentCollectionId.NewId();
        var docId = DocumentId.NewId();

        var collection1 = await store.OpenDocumentCollection(collectionId);
        await collection1.Update(docId, new TestViewModel { Name = "Shared" });

        var collection2 = await store.OpenDocumentCollection(collectionId);
        var result = await collection2.GetById(docId);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Shared");
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var store = CreateStore();

        Should.NotThrow(() => store.Dispose());
    }

    [Fact]
    public async Task DisposeAsync_DoesNotThrow()
    {
        var store = CreateStore();

        await Should.NotThrowAsync(async () => await store.DisposeAsync());
    }
}
