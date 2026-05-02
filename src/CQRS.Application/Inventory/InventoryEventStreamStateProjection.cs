using CQRS.Domain.Inventory;
using CQRS.Ports.EventStore;

namespace CQRS.Application.Inventory;

public sealed class InventoryEventStreamStateProjection
    : IEventStreamProjection<InventoryState, IInventoryEvent>
{
    public InventoryState GetInitialState(EventStreamId streamId) =>
        InventoryStateProjection.GetInitialState(InventoryEventStreamId.GetInventoryId(streamId));

    public InventoryState Apply(InventoryState state, IInventoryEvent evt) =>
        InventoryStateProjection.Apply(state, evt);
}
