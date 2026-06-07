using CQRS.Domain.Inventory;
using CQRS.Ports.EventStore;

namespace CQRS.Application.Inventory;

public static class InventoryEventStreamId
{
    public const string EventStreamPrefix = "Inventory_";

    public static EventStreamId GetStreamId(InventoryId inventoryId) =>
        (EventStreamId)$"{EventStreamPrefix}{inventoryId}";

    public static string GetDocumentId(string streamKey) => streamKey[EventStreamPrefix.Length..];
}
