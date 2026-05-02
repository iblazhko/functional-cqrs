using Wolverine;
using WolverineIMessageBus = Wolverine.IMessageBus;

namespace CQRS.Adapters.WolverineMessageBus.Tests;

public sealed class FakeWolverineMessageBus : WolverineIMessageBus
{
    public List<(object Message, DeliveryOptions? Options)> Published { get; } = [];
    public List<(object Message, DeliveryOptions? Options)> Sent { get; } = [];

    public ValueTask BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options = null)
    {
        throw new NotImplementedException();
    }

    public string? TenantId { get; set; }

    public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        Published.Add((message, options));
        return ValueTask.CompletedTask;
    }

    public Task InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation = new CancellationToken(),
        TimeSpan? timeout = null)
    {
        throw new NotImplementedException();
    }

    public Task<T> InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellation = new CancellationToken(),
        TimeSpan? timeout = null)
    {
        throw new NotImplementedException();
    }

    public IDestinationEndpoint EndpointFor(string endpointName)
    {
        throw new NotImplementedException();
    }

    public IDestinationEndpoint EndpointFor(Uri uri)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<Envelope> PreviewSubscriptions(object message, DeliveryOptions options)
    {
        throw new NotImplementedException();
    }

    public ValueTask SendAsync<T>(T message, DeliveryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        Sent.Add((message, options));
        return ValueTask.CompletedTask;
    }

    public Task InvokeAsync(object message, CancellationToken cancellation = new CancellationToken(), TimeSpan? timeout = null)
    {
        throw new NotImplementedException();
    }

    public Task InvokeAsync(object message, DeliveryOptions options, CancellationToken cancellation = new CancellationToken(),
        TimeSpan? timeout = null)
    {
        throw new NotImplementedException();
    }

    public Task<T> InvokeAsync<T>(object message, CancellationToken cancellation = new CancellationToken(), TimeSpan? timeout = null)
    {
        throw new NotImplementedException();
    }

    public Task<T> InvokeAsync<T>(object message, DeliveryOptions options, CancellationToken cancellation = new CancellationToken(),
        TimeSpan? timeout = null)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(object message, CancellationToken cancellation = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(object message, DeliveryOptions options,
        CancellationToken cancellation = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}
