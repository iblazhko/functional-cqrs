using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.Mapping.Inventory;
using CQRS.Ports.EventStore;
using LanguageExt;

namespace CQRS.Application.Inventory;

public sealed class EventStoreInventoryEventMapper
    : Ports.EventStore.IEventMapper<IInventoryEvent, IInventoryEventDto>
{
    public IInventoryEventDto ToEventDto(IInventoryEvent domain) => eventMapper.ToEventDto(domain);

    public Either<EventDeserializationError, IInventoryEvent> ToDomainEvent(
        IInventoryEventDto dto
    ) =>
        eventMapper
            .ToDomainEvent(dto)
            .MapLeft(fault => new EventDeserializationError(
                fault.FromType,
                string.Join("; ", fault.Errors)
            ));

    private static readonly IInventoryEventMapper eventMapper =
        new Mapping.Inventory.V1.InventoryEventV1Mapper();
}
