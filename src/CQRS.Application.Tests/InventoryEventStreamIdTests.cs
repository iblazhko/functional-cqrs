using CQRS.Application.Inventory;
using CQRS.Domain.Inventory;
using CQRS.Ports.EventStore;
using Shouldly;

namespace CQRS.Application.Tests;

public sealed class InventoryEventStreamIdTests
{
    [Fact]
    public void GetStreamId_ReturnsPrefixedStreamId()
    {
        var inventoryId = InventoryId.NewId();

        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);

        ((string)streamId).ShouldStartWith("Inventory_");
    }

    [Fact]
    public void GetStreamId_StreamIdContainsInventoryId()
    {
        var inventoryId = InventoryId.NewId();

        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);

        ((string)streamId).ShouldContain((string)inventoryId);
    }

    [Fact]
    public void GetStreamId_DifferentIds_ProduceDifferentStreamIds()
    {
        var id1 = InventoryId.NewId();
        var id2 = InventoryId.NewId();

        var streamId1 = InventoryEventStreamId.GetStreamId(id1);
        var streamId2 = InventoryEventStreamId.GetStreamId(id2);

        streamId1.ShouldNotBe(streamId2);
    }
}
