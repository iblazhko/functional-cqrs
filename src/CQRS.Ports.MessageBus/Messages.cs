namespace CQRS.Ports.MessageBus;

public readonly record struct MessagingId(Guid Id)
{
    public static MessagingId NewId() => new(Guid.NewGuid());

    public static MessagingId Empty { get; } = new(Guid.Empty);

    public static implicit operator Guid(MessagingId messagingId) => messagingId.Id;

    public static explicit operator MessagingId(Guid messagingId) => new(messagingId);

    public static explicit operator MessagingId?(Guid? messagingId) =>
        messagingId is { } id ? new(id) : null;
}

public sealed record Context
{
    public required MessagingId MessageId { get; init; }
    public required MessagingId CorrelationId { get; init; }
    public MessagingId? CausationId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public static Context Empty { get; } =
        new()
        {
            MessageId = MessagingId.Empty,
            CorrelationId = MessagingId.Empty,
            CausationId = null,
            Timestamp = default,
        };

    public static Context GetNew(DateTimeOffset timestamp) =>
        new()
        {
            MessageId = MessagingId.NewId(),
            CorrelationId = MessagingId.NewId(),
            CausationId = null,
            Timestamp = timestamp,
        };
}

public static class MessageContextBuilderExtensions
{
    public static Context GetResponseMetadata(this Context context, DateTimeOffset timestamp) =>
        new()
        {
            MessageId = MessagingId.NewId(),
            CorrelationId = context.CorrelationId,
            CausationId = context.MessageId,
            Timestamp = timestamp,
        };
}
