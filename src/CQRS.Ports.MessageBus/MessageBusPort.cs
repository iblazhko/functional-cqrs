namespace CQRS.Ports.MessageBus;

public sealed class PermanentProcessingFailureException(string message, Exception innerException)
    : Exception(message, innerException);

public interface IPublishEvent
{
    Task Publish<T>(T message, Context context, CancellationToken cancellationToken = default);
}

public interface ISendCommand
{
    Task Send<T>(T message, Context context, CancellationToken cancellationToken = default);
}

public interface IMessageBus : IPublishEvent, ISendCommand;
