using System.Text.Json;
using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Application.Inventory;
using CQRS.Domain;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.Ports.MessageBus;
using Serilog;
using Wolverine;

namespace CQRS.Application.WolverineHandlers;

public sealed class InventoryCommandConsumer(
    ITimeProvider timeProvider,
    IMoonPhaseService moonPhaseService,
    ICommandProcessingStatusRecordingService commandProcessingStatusRecorder,
    InventoryCommandDtoHandler handler
)
{
    public Task Consume(CreateInventoryCommand message, Envelope envelope) =>
        Handle(message, envelope);

    public Task Consume(RenameInventoryCommand message, Envelope envelope) =>
        Handle(message, envelope);

    public Task Consume(AddItemsToInventoryCommand message, Envelope envelope) =>
        Handle(message, envelope);

    public Task Consume(RemoveItemsFromInventoryCommand message, Envelope envelope) =>
        Handle(message, envelope);

    public Task Consume(DeactivateInventoryCommand message, Envelope envelope) =>
        Handle(message, envelope);

    private async Task Handle<T>(T message, Envelope envelope)
        where T : class, IInventoryCommandDto
    {
        var moonPhase = await moonPhaseService.GetMoonPhase(
            timeProvider.TimeZone,
            timeProvider.GetUtcNow()
        );
        var context = new Context
        {
            MessageId = (MessagingId)(TryParseHeader(envelope, "cqrs-message-id") ?? envelope.Id),
            CorrelationId = (MessagingId)(TryParseHeader(envelope, "cqrs-correlation-id") ?? TryParseGuid(envelope.CorrelationId) ?? envelope.Id),
            CausationId = (TryParseHeader(envelope, "cqrs-causation-id") ?? TryParseGuid(envelope.ParentId)) is { } g ? (MessagingId)g : null,
            Timestamp = timeProvider.GetUtcNow(),
        };
        await InvokeWithProcessingStatusRecording(message, context, moonPhase);
    }

    private static Guid? TryParseHeader(Envelope envelope, string key) =>
        envelope.Headers?.TryGetValue(key, out var value) == true ? TryParseGuid(value) : null;

    private async Task InvokeWithProcessingStatusRecording<T>(
        T message,
        Context context,
        MoonPhase moonPhase
    )
        where T : class, IInventoryCommandDto
    {
        Log.Information(
            "[MESSAGE-BUS<-] {MessageType} {@Message}",
            message.GetType().FullName,
            message
        );

        var commandId = context.MessageId.Id;
        var correlationId = context.CorrelationId.Id;
        var causationId = context.CausationId?.Id ?? Guid.Empty;

        var request = new CommandProcessingRequest(
            commandId,
            correlationId,
            causationId,
            message.GetType().FullName ?? "N/A",
            JsonSerializer.Serialize(message, SerializeOptions),
            context.Timestamp
        );

        await commandProcessingStatusRecorder.RecordCommandProcessingStarted(request);

        try
        {
            var result = await handler.Handle(message, moonPhase, correlationId, commandId);

            await result.Match(
                completed =>
                    commandProcessingStatusRecorder.RecordCommandProcessingCompleted(
                        request.CommandId,
                        timeProvider.GetUtcNow(),
                        $"New events: [{string.Join(", ", completed.NewEvents.Select(x => x.GetType().Name))}]"
                    ),
                rejected =>
                    commandProcessingStatusRecorder.RecordCommandProcessingRejected(
                        request.CommandId,
                        timeProvider.GetUtcNow(),
                        rejected.Fault.ToString()
                    ),
                failed =>
                    commandProcessingStatusRecorder.RecordCommandProcessingFailed(
                        request.CommandId,
                        timeProvider.GetUtcNow(),
                        failed.GetType().Name
                    ),
                deserializationFailed =>
                    commandProcessingStatusRecorder.RecordCommandProcessingFailed(
                        request.CommandId,
                        timeProvider.GetUtcNow(),
                        $"Event deserialization failed for '{deserializationFailed.Error.EventType}': {deserializationFailed.Error.Reason}"
                    )
            );
        }
        catch (Exception ex)
        {
            await commandProcessingStatusRecorder.RecordCommandProcessingFailed(
                request.CommandId,
                timeProvider.GetUtcNow(),
                ex.Message
            );

            throw;
        }
    }

    private static Guid? TryParseGuid(string? value) =>
        Guid.TryParse(value, out var g) ? g : null;

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        WriteIndented = false,
    };
}
