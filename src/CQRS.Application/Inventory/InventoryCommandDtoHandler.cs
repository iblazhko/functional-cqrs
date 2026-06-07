using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.IntegrationEvents.Inventory.V1;
using CQRS.Mapping;
using CQRS.Mapping.Inventory;
using CQRS.Ports.EventStore;
using CQRS.Ports.MessageBus;
using LanguageExt;
using OneOf;

namespace CQRS.Application.Inventory;

public class InventoryCommandDtoHandler(
    IEventStore<Option<InventoryState>, IInventoryEvent, IInventoryEventDto> eventStore,
    IInventoryCommandMapper commandMapper,
    InventoryEventStreamStateProjection stateProjection,
    EventStoreInventoryEventMapper eventMapper,
    IMessageBus bus,
    ITimeProvider timeProvider
)
{
    public Task<CommandProcessingResult> Handle(
        IInventoryCommandDto dto,
        MoonPhase moonPhase,
        Guid correlationId,
        Guid causationId
    ) =>
        commandMapper
            .ToDomainCommand(dto)
            .Match(
                fault => Task.FromResult(CommandProcessingResult.Rejected(fault)),
                command => Handle(command, moonPhase, correlationId, causationId)
            );

    private async Task<CommandProcessingResult> Handle(
        IInventoryCommand command,
        MoonPhase moonPhase,
        Guid correlationId,
        Guid causationId
    )
    {
        var streamId = InventoryEventStreamId.GetStreamId(command.Id);
        await using var session = eventStore.Open(streamId, eventMapper);
        var stateResult = await session.GetState(stateProjection);

        return await stateResult.Match(
            error => Task.FromResult(CommandProcessingResult.DeserializationFailed(error)),
            currentState =>
            {
                var commandHandlingResult = InventoryCommandHandler.Handle(
                    currentState,
                    command,
                    moonPhase
                );

                return commandHandlingResult.Match(
                    error => Task.FromResult(CommandProcessingResult.Failed(error)),
                    SaveNewEvents
                );

                async Task<CommandProcessingResult> SaveNewEvents(Seq<IInventoryEvent> newEvents)
                {
                    session.AppendEvents(newEvents, correlationId, causationId);
                    await session.Save();
                    await PublishIntegrationEvent(
                        newEvents,
                        currentState,
                        correlationId,
                        causationId
                    );
                    return CommandProcessingResult.Completed(newEvents);
                }
            }
        );
    }

    private async Task PublishIntegrationEvent(
        Seq<IInventoryEvent> newEvents,
        Option<InventoryState> stateBefore,
        Guid correlationId,
        Guid causationId
    )
    {
        var newState = newEvents.Fold(stateBefore, InventoryStateProjection.Apply);
        await newState.Match(
            Some: state =>
                bus.Publish(
                    new InventoryUpdated
                    {
                        InventoryId = state.Id,
                        Name = state.Name,
                        StockQuantity = state.Quantity.Match(q => (int)q, () => 0),
                        IsActive = state.IsActive,
                    },
                    new Context
                    {
                        MessageId = MessagingId.NewId(),
                        CorrelationId = (MessagingId)correlationId,
                        CausationId = (MessagingId)causationId,
                        Timestamp = timeProvider.GetUtcNow(),
                    }
                ),
            None: () => Task.CompletedTask
        );
    }
}

public sealed class CommandProcessingResult
    : OneOfBase<
        CommandProcessingResult.CompletedResult,
        CommandProcessingResult.RejectedResult,
        CommandProcessingResult.FailedResult,
        CommandProcessingResult.DeserializationFailedResult
    >
{
    public record CompletedResult(Seq<IInventoryEvent> NewEvents);

    public record RejectedResult(MappingFault Fault);

    public record FailedResult(InventoryAggregate.Errors.IInventoryCommandError Error);

    public record DeserializationFailedResult(EventDeserializationError Error);

    public static CommandProcessingResult Completed(Seq<IInventoryEvent> newEvents) =>
        new(new CompletedResult(newEvents));

    public static CommandProcessingResult Rejected(MappingFault fault) =>
        new(new RejectedResult(fault));

    public static CommandProcessingResult Failed(
        InventoryAggregate.Errors.IInventoryCommandError error
    ) => new(new FailedResult(error));

    public static CommandProcessingResult DeserializationFailed(EventDeserializationError error) =>
        new(new DeserializationFailedResult(error));

    private CommandProcessingResult(
        OneOf<CompletedResult, RejectedResult, FailedResult, DeserializationFailedResult> input
    )
        : base(input) { }
}
