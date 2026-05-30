using CQRS.Domain.Inventory;
using CQRS.Ports.EventStore;
using LanguageExt;

namespace CQRS.Application.Inventory;

public sealed class InventoryEventStreamStateProjection
    : IEventStreamProjection<Option<InventoryState>, IInventoryEvent>
{
    public Option<InventoryState> GetInitialState(EventStreamId streamId) =>
        InventoryStateProjection.GetInitialState();

    public Option<InventoryState> Apply(Option<InventoryState> state, IInventoryEvent evt) =>
        InventoryStateProjection.Apply(state, evt);
}
