using CQRS.Domain.Inventory;
using CQRS.Ports.EventStore;

namespace CQRS.Application.Inventory;

public static class InventoryEventStreamId
{
    public static EventStreamId GetStreamId(InventoryId inventoryId) =>
        (EventStreamId)$"{EventStreamPrefix}{inventoryId}";

    private const string EventStreamPrefix = "Inventory_";
}
