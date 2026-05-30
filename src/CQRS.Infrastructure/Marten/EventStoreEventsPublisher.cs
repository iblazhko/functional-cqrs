namespace CQRS.Infrastructure;

using CQRS.Domain.Inventory;
using CQRS.Mapping.Inventory.V1;
using CQRS.Ports.EventStore;
using CQRS.Ports.MessageBus;

public class EventStoreEventsPublisher<T>(IMessageBus bus) : IEventPublisher<T>
    where T : IInventoryEvent
{
    public async Task Publish(
        IEnumerable<EventWithMetadata<T>> events,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var evt in events)
        {
            await PublishEventDto(
                evt.Event,
                bus,
                new Context
                {
                    MessageId = (MessagingId)evt.Metadata.EventId,
                    CorrelationId = (MessagingId)evt.Metadata.CorrelationId,
                    CausationId = (MessagingId?)evt.Metadata.CausationId,
                    Timestamp = evt.Metadata.Timestamp,
                },
                cancellationToken
            );
        }
    }

    private static Task PublishEventDto(
        IInventoryEvent @event,
        IPublishEvent bus,
        Context context,
        CancellationToken ct
    ) =>
        @event switch
        {
            InventoryCreated x => bus.Publish(x.ToDto(), context, ct),
            InventoryRenamed x => bus.Publish(x.ToDto(), context, ct),
            ItemsAddedToInventory x => bus.Publish(x.ToDto(), context, ct),
            ItemsRemovedFromInventory x => bus.Publish(x.ToDto(), context, ct),
            ItemWentInStock x => bus.Publish(x.ToDto(), context, ct),
            ItemWentOutOfStock x => bus.Publish(x.ToDto(), context, ct),
            InventoryDeactivated x => bus.Publish(x.ToDto(), context, ct),
            _ => throw new InvalidOperationException(
                $"Unknown domain event type: {@event.GetType().FullName ?? @event.GetType().Name}"
            ),
        };
}
