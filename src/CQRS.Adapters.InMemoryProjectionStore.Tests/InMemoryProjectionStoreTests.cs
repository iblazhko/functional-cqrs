using CQRS.Ports.ProjectionStore;
using Shouldly;

namespace CQRS.Adapters.InMemoryProjectionStore.Tests;

internal class TestViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class InMemoryDocumentCollectionTests
{
    private readonly InMemoryDocumentCollection<TestViewModel> _collection = new();

    [Fact]
    public async Task GetById_ForUnknownDocument_ReturnsNull()
    {
        var result = await _collection.GetById((DocumentId)"unknown");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Update_WithViewModel_StoresDocument()
    {
        var id = (DocumentId)"doc-1";

        await _collection.Update(id, new TestViewModel { Name = "Test", Count = 42 });

        var result = await _collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Count.ShouldBe(42);
    }

    [Fact]
    public async Task Update_WithViewModel_OverwritesExistingDocument()
    {
        var id = (DocumentId)"doc-overwrite";
        await _collection.Update(id, new TestViewModel { Name = "Original", Count = 1 });

        await _collection.Update(id, new TestViewModel { Name = "Updated", Count = 2 });

        var result = await _collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated");
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Update_WithFunc_ForNewDocument_CreatesDefaultAndAppliesFunc()
    {
        var id = (DocumentId)"doc-func-new";

        await _collection.Update(id, vm => { vm.Name = "FromFunc"; vm.Count = 10; return vm; });

        var result = await _collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("FromFunc");
        result.Count.ShouldBe(10);
    }

    [Fact]
    public async Task Update_WithFunc_ForExistingDocument_AppliesFuncToExistingVm()
    {
        var id = (DocumentId)"doc-func-existing";
        await _collection.Update(id, new TestViewModel { Name = "Original", Count = 5 });

        await _collection.Update(id, vm => { vm.Count += 3; return vm; });

        var result = await _collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Original");
        result.Count.ShouldBe(8);
    }

    [Fact]
    public async Task Update_WithFunc_MultipleUpdates_AccumulatesChanges()
    {
        var id = (DocumentId)"doc-accumulate";

        await _collection.Update(id, vm => { vm.Count = 1; return vm; });
        await _collection.Update(id, vm => { vm.Count += 2; return vm; });
        await _collection.Update(id, vm => { vm.Count += 3; return vm; });

        var result = await _collection.GetById(id);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(6);
    }

    [Fact]
    public async Task GetById_WithMixedCaseDocumentId_FindsDocument()
    {
        await _collection.Update((DocumentId)"MyDoc", new TestViewModel { Name = "Stored" });

        var result = await _collection.GetById((DocumentId)"mydoc");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Stored");
    }
}

public sealed class InMemoryProjectionStoreAdapterTests
{
    private readonly InMemoryProjectionStoreAdapter<TestViewModel> _store = new();

    [Fact]
    public async Task OpenDocumentCollection_ReturnsNonNullCollection()
    {
        var collection = await _store.OpenDocumentCollection(DocumentCollectionId.NewId());

        collection.ShouldNotBeNull();
    }

    [Fact]
    public async Task OpenDocumentCollection_SameCollectionId_ReturnsSameCollection()
    {
        var collectionId = DocumentCollectionId.NewId();
        var docId = DocumentId.NewId();

        var collection1 = await _store.OpenDocumentCollection(collectionId);
        await collection1.Update(docId, new TestViewModel { Name = "Shared" });

        var collection2 = await _store.OpenDocumentCollection(collectionId);
        var result = await collection2.GetById(docId);

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Shared");
    }

    [Fact]
    public async Task OpenDocumentCollection_DifferentCollectionIds_ReturnsSeparateCollections()
    {
        var docId = DocumentId.NewId();

        var collection1 = await _store.OpenDocumentCollection(DocumentCollectionId.NewId());
        await collection1.Update(docId, new TestViewModel { Name = "OnlyInCollection1" });

        var collection2 = await _store.OpenDocumentCollection(DocumentCollectionId.NewId());
        var result = await collection2.GetById(docId);

        result.ShouldBeNull();
    }
}
