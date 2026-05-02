using CQRS.Domain.Inventory;
using CQRS.EntityIds;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.Inventory.V1;
using Shouldly;

namespace CQRS.Projections.Tests;

public sealed class InventoryProjectionDocumentIdTests
{
    [Fact]
    public void FromInventoryId_ReturnsDocumentIdMatchingInventoryId()
    {
        var inventoryId = InventoryId.NewId();

        var documentId = InventoryProjectionDocumentId.FromInventoryId(inventoryId);

        ((string)documentId).ShouldBe((string)inventoryId);
    }

    [Fact]
    public void FromInventoryId_DifferentIds_ProduceDifferentDocumentIds()
    {
        var id1 = InventoryId.NewId();
        var id2 = InventoryId.NewId();

        var doc1 = InventoryProjectionDocumentId.FromInventoryId(id1);
        var doc2 = InventoryProjectionDocumentId.FromInventoryId(id2);

        doc1.ShouldNotBe(doc2);
    }
}
