using CQRS.Ports.ProjectionStore;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace CQRS.Adapters.MartenDbProjectionStore.Tests;

public class TestViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

[Collection("PostgreSQL Integration")]
public sealed class MartenDbDocumentCollectionTests(PostgreSqlContainerFixture fixture)
{
    private MartenDbDocumentCollection<TestViewModel> CreateCollection() =>
        new(fixture.DocumentStore, NullLogger<MartenDbDocumentCollection<TestViewModel>>.Instance);

    [Fact]
    public async Task GetById_ForUnknownDocument_ReturnsNull()
    {
        var collection = CreateCollection();

        var result = await collection.GetById(DocumentId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Update_WithViewModel_StoresDocument()
    {
        var collection = CreateCollection();
        var id = DocumentId.NewId();

        await collection.Update(id, new TestViewModel { Name = "Test", Count = 42 });

        var result = await collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test");
        result.Count.ShouldBe(42);
    }

    [Fact]
    public async Task Update_WithViewModel_OverwritesExistingDocument()
    {
        var collection = CreateCollection();
        var id = DocumentId.NewId();
        await collection.Update(id, new TestViewModel { Name = "Original", Count = 1 });

        await collection.Update(id, new TestViewModel { Name = "Updated", Count = 2 });

        var result = await collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated");
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Update_WithFunc_ForNewDocument_CreatesDefaultAndAppliesFunc()
    {
        var collection = CreateCollection();
        var id = DocumentId.NewId();

        await collection.Update(
            id,
            vm =>
            {
                vm.Name = "FromFunc";
                vm.Count = 10;
                return vm;
            }
        );

        var result = await collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("FromFunc");
        result.Count.ShouldBe(10);
    }

    [Fact]
    public async Task Update_WithFunc_ForExistingDocument_AppliesFuncToExistingVm()
    {
        var collection = CreateCollection();
        var id = DocumentId.NewId();
        await collection.Update(id, new TestViewModel { Name = "Original", Count = 5 });

        await collection.Update(
            id,
            vm =>
            {
                vm.Count += 3;
                return vm;
            }
        );

        var result = await collection.GetById(id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Original");
        result.Count.ShouldBe(8);
    }

    [Fact]
    public async Task Update_WithFunc_MultipleUpdates_AccumulatesChanges()
    {
        var collection = CreateCollection();
        var id = DocumentId.NewId();

        await collection.Update(
            id,
            vm =>
            {
                vm.Count = 1;
                return vm;
            }
        );
        await collection.Update(
            id,
            vm =>
            {
                vm.Count += 2;
                return vm;
            }
        );
        await collection.Update(
            id,
            vm =>
            {
                vm.Count += 3;
                return vm;
            }
        );

        var result = await collection.GetById(id);
        result.ShouldNotBeNull();
        result.Count.ShouldBe(6);
    }

    [Fact]
    public async Task Update_WithViewModel_IncrementsVersion()
    {
        var collection = CreateCollection();
        var id = DocumentId.NewId();

        await collection.Update(id, new TestViewModel { Name = "v1" });
        await collection.Update(id, new TestViewModel { Name = "v2" });

        using var session = fixture.DocumentStore.LightweightSession();
        var envelope = await session.LoadAsync<DocumentEnvelope<TestViewModel>>(
            id,
            token: TestContext.Current.CancellationToken
        );
        envelope.ShouldNotBeNull();
        ((long)envelope.Version).ShouldBe(2);
    }

    [Fact]
    public async Task Update_NewDocument_StartsAtVersionOne()
    {
        var collection = CreateCollection();
        var id = DocumentId.NewId();

        await collection.Update(id, new TestViewModel { Name = "first" });

        using var session = fixture.DocumentStore.LightweightSession();
        var envelope = await session.LoadAsync<DocumentEnvelope<TestViewModel>>(
            id,
            token: TestContext.Current.CancellationToken
        );
        envelope.ShouldNotBeNull();
        ((long)envelope.Version).ShouldBe(1);
    }
}
