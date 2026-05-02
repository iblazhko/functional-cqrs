using CQRS.Ports.MessageBus;

namespace CQRS.API.Tests;

public sealed class FakeMessageBus : IMessageBus
{
    public List<(object Message, Context Context)> SentMessages { get; } = [];

    public Task Publish<T>(T message, Context context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        SentMessages.Add((message, context));
        return Task.CompletedTask;
    }

    public Task Send<T>(T message, Context context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        SentMessages.Add((message, context));
        return Task.CompletedTask;
    }
}
