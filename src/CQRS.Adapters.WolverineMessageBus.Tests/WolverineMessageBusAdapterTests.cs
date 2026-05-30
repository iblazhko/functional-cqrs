using CQRS.Ports.MessageBus;
using Shouldly;

namespace CQRS.Adapters.WolverineMessageBus.Tests;

public sealed class WolverineMessageBusAdapterTests
{
    private readonly FakeWolverineMessageBus _bus = new();
    private readonly WolverineMessageBusAdapter _adapter;

    public WolverineMessageBusAdapterTests()
    {
        _adapter = new WolverineMessageBusAdapter(_bus);
    }

    private static Context MakeContext(bool withCausation = false) =>
        new()
        {
            MessageId = MessagingId.NewId(),
            CorrelationId = MessagingId.NewId(),
            CausationId = withCausation ? MessagingId.NewId() : null,
            Timestamp = DateTimeOffset.UtcNow,
        };

    // --- Publish<T>(T message, Context context) ---

    private interface ITestEvent;
    private sealed record ConcreteTestEvent(string Value) : ITestEvent;

    [Fact]
    public async Task Publish_UsesConcreteRuntimeType_NotInterface()
    {
        ITestEvent message = new ConcreteTestEvent("x");

        await _adapter.Publish(message, MakeContext(), cancellationToken: TestContext.Current.CancellationToken);

        var (published, _) = _bus.Published.ShouldHaveSingleItem();
        published.GetType().ShouldBe(typeof(ConcreteTestEvent));
    }

    [Fact]
    public async Task Publish_NullMessage_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => _adapter.Publish<string>(null!, MakeContext())
        );
    }

    [Fact]
    public async Task Publish_MessageIsForwardedToBus()
    {
        var message = "test-message";

        await _adapter.Publish(message, MakeContext(), cancellationToken: TestContext.Current.CancellationToken);

        var (published, _) = _bus.Published.ShouldHaveSingleItem();
        published.ShouldBe(message);
    }

    [Fact]
    public async Task Publish_SetsMessageIdHeader()
    {
        var context = MakeContext();

        await _adapter.Publish("hello", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Published.ShouldHaveSingleItem();
        options!.Headers["cqrs-message-id"].ShouldBe(context.MessageId.Id.ToString());
    }

    [Fact]
    public async Task Publish_SetsCorrelationIdHeader()
    {
        var context = MakeContext();

        await _adapter.Publish("hello", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Published.ShouldHaveSingleItem();
        options!.Headers["cqrs-correlation-id"].ShouldBe(context.CorrelationId.Id.ToString());
    }

    [Fact]
    public async Task Publish_WithCausationId_SetsParentIdHeader()
    {
        var context = MakeContext(withCausation: true);

        await _adapter.Publish("hello", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Published.ShouldHaveSingleItem();
        options!.Headers["cqrs-causation-id"].ShouldBe(context.CausationId!.Value.Id.ToString());
    }

    [Fact]
    public async Task Publish_WithoutCausationId_NoParentIdHeader()
    {
        var context = MakeContext(withCausation: false);

        await _adapter.Publish("hello", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Published.ShouldHaveSingleItem();
        options!.Headers.ContainsKey("cqrs-causation-id").ShouldBeFalse();
    }

    // --- Send<T>(T message, Context context) ---

    [Fact]
    public async Task Send_NullMessage_ThrowsArgumentNullException()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            () => _adapter.Send<string>(null!, MakeContext())
        );
    }

    [Fact]
    public async Task Send_MessageIsForwardedToBus()
    {
        var message = "cmd-payload";

        await _adapter.Send(message, MakeContext(), cancellationToken: TestContext.Current.CancellationToken);

        var (sent, _) = _bus.Sent.ShouldHaveSingleItem();
        sent.ShouldBe(message);
    }

    [Fact]
    public async Task Send_SetsMessageIdHeader()
    {
        var context = MakeContext();

        await _adapter.Send("cmd", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Sent.ShouldHaveSingleItem();
        options!.Headers["cqrs-message-id"].ShouldBe(context.MessageId.Id.ToString());
    }

    [Fact]
    public async Task Send_SetsCorrelationIdHeader()
    {
        var context = MakeContext();

        await _adapter.Send("cmd", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Sent.ShouldHaveSingleItem();
        options!.Headers["cqrs-correlation-id"].ShouldBe(context.CorrelationId.Id.ToString());
    }

    [Fact]
    public async Task Send_WithCausationId_SetsParentIdHeader()
    {
        var context = MakeContext(withCausation: true);

        await _adapter.Send("cmd", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Sent.ShouldHaveSingleItem();
        options!.Headers["cqrs-causation-id"].ShouldBe(context.CausationId!.Value.Id.ToString());
    }

    [Fact]
    public async Task Send_WithoutCausationId_NoParentIdHeader()
    {
        var context = MakeContext(withCausation: false);

        await _adapter.Send("cmd", context, cancellationToken: TestContext.Current.CancellationToken);

        var (_, options) = _bus.Sent.ShouldHaveSingleItem();
        options!.Headers.ContainsKey("cqrs-causation-id").ShouldBeFalse();
    }
}
