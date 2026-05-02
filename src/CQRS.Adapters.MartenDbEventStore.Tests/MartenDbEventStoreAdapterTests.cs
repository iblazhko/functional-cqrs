using CQRS.Application.Inventory;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.Ports.EventStore;
using Shouldly;

namespace CQRS.Adapters.MartenDbEventStore.Tests;

[Collection("PostgreSQL Integration")]
public sealed class MartenDbEventStoreAdapterTests(PostgreSqlContainerFixture fixture)
{
    private readonly IEventMapper<IInventoryEvent, IInventoryEventDto> _mapper =
        new EventStoreInventoryEventMapper();

    [Fact]
    public async Task Contains_ThrowsNotImplementedException()
    {
        var adapter = fixture.CreateAdapter();
        var streamId = InventoryEventStreamId.GetStreamId(InventoryId.NewId());

        await Should.ThrowAsync<NotImplementedException>(() => adapter.Contains(streamId));
    }

    [Fact]
    public async Task Delete_ThrowsNotImplementedException()
    {
        var adapter = fixture.CreateAdapter();
        var streamId = InventoryEventStreamId.GetStreamId(InventoryId.NewId());

        await Should.ThrowAsync<NotImplementedException>(() => adapter.Delete(streamId));
    }

    [Fact]
    public void Open_ReturnsNonNullSession()
    {
        var adapter = fixture.CreateAdapter();
        var streamId = InventoryEventStreamId.GetStreamId(InventoryId.NewId());

        using var session = adapter.Open(streamId, _mapper);

        session.ShouldNotBeNull();
    }
}
