using Wolverine;
using WolverineIMessageBus = Wolverine.IMessageBus;

namespace CQRS.Adapters.WolverineMessageBus;

public sealed class WolverineMessageBusAdapter(WolverineIMessageBus bus)
    : Ports.MessageBus.IMessageBus
{
    public Task Publish<T>(
        T message,
        Ports.MessageBus.Context context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        return bus.PublishAsync(message, context.ToDeliveryOptions()).AsTask();
    }

    public Task Send<T>(
        T message,
        Ports.MessageBus.Context context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(message);
        return bus.SendAsync(message, context.ToDeliveryOptions()).AsTask();
    }
}

public static class WolverineMessageBusAdapterExtensions
{
    public static DeliveryOptions ToDeliveryOptions(this Ports.MessageBus.Context context)
    {
        var opts = new DeliveryOptions();
        opts.Headers["cqrs-message-id"] = context.MessageId.Id.ToString();
        opts.Headers["cqrs-correlation-id"] = context.CorrelationId.Id.ToString();
        if (context.CausationId is { } causationId)
            opts.Headers["cqrs-causation-id"] = causationId.Id.ToString();
        return opts;
    }
}
