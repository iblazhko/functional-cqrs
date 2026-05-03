using LanguageExt;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.Mapping;
using CQRS.Ports.ProjectionStore;
using CQRS.Projections.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using Microsoft.Extensions.Logging;

namespace CQRS.Projections.WolverineHandlers;

public sealed class InventoryEventConsumer(
    IProjectionStore<InventoryViewModel> projectionStore,
    ILogger<InventoryEventConsumer> logger
)
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
        logger.LogInformation(
            "[MESSAGE-BUS] {MessageType} {@Message}",
            eventDto.GetType().FullName,
            eventDto
        );

        var result = await InventoryEventDtoHandler.Handle(eventDto, GetHandlerContext());
        result.IfLeft(fault => throw new MappingException(fault.FromType, fault.ToType, fault.Message));
    }

    private IInventoryDomainEventDtoHandlerContext<InventoryViewModel> GetHandlerContext() =>
        new InventoryInventoryDomainEventDtoHandlerContext<InventoryViewModel>(
            projectionStore,
            InventoryViewModelProjection.Apply
        );
}
