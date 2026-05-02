using CQRS.Domain.Inventory;
using CQRS.EntityIds;
using CQRS.Ports.EventStore;

namespace CQRS.Application.Inventory;

public static class InventoryEventStreamId
{
    public static EventStreamId GetStreamId(InventoryId inventoryId) =>
        (EventStreamId)$"{EventStreamPrefix}{inventoryId}";

    public static InventoryId GetInventoryId(EventStreamId eventStreamId) =>
        InventoryId.Create(
            EntityId.Create(((string)eventStreamId)[EventStreamPrefix.Length..])
                .Match(
                    Right: id => id,
                    Left: _ => throw new InvalidOperationException($"Invalid event stream identifier '{(string)eventStreamId}'")
                )
            );

    private const string EventStreamPrefix = "Inventory_";
}
