using LanguageExt;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.Mapping;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using Serilog;

namespace CQRS.Projections.WolverineHandlers;

public sealed class InventoryEventConsumer(IProjectionStore<InventoryViewModel> projectionStore)
{
    public Task Consume(InventoryCreatedEvent message) => HandleEventDto(message);

    public Task Consume(InventoryRenamedEvent message) => HandleEventDto(message);

    public Task Consume(ItemsAddedToInventoryEvent message) => HandleEventDto(message);

    public Task Consume(ItemsRemovedFromInventoryEvent message) => HandleEventDto(message);

    public Task Consume(InventoryDeactivatedEvent message) => HandleEventDto(message);

    public Task Consume(ItemWentInStockEvent message) => HandleEventDto(message);

    public Task Consume(ItemWentOutOfStockEvent message) => HandleEventDto(message);

    private async Task HandleEventDto<TEventDto>(TEventDto eventDto)
        where TEventDto : class, IInventoryEventDto
    {
        Log.Logger.Information(
            "[MESSAGE-BUS] {MessageType} {@Message}",
            eventDto.GetType().FullName,
            eventDto
        );

        var result = await InventoryEventDtoHandler.Handle(eventDto, GetHandlerContext());
        _ = result.Match(
            Left: fault => throw new MappingException(fault.FromType, fault.ToType, fault.Message),
            Right: _ => Unit.Default
        );
    }

    private IInventoryDomainEventDtoHandlerContext<InventoryViewModel> GetHandlerContext() =>
        new InventoryInventoryDomainEventDtoHandlerContext<InventoryViewModel>(
            projectionStore,
            InventoryViewModelProjection.Apply
        );
}
