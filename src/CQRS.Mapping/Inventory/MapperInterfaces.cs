using CQRS.Domain.Inventory;
using CQRS.DTO;
using LanguageExt;

namespace CQRS.Mapping.Inventory;

public interface IInventoryCommandMapper
{
    IInventoryCommandDto ToCommandDto(IInventoryCommand domain);
    Either<MappingFault, IInventoryCommand> ToDomainCommand(IInventoryCommandDto dto);
}

public interface IInventoryEventMapper
{
    IInventoryEventDto ToEventDto(IInventoryEvent domain);
    Either<MappingFault, IInventoryEvent> ToDomainEvent(IInventoryEventDto dto);
}

public static class InventoryCommandMapperExtensions
{
    public static Either<MappingFault, IInventoryCommand> ToBaseCommand<TInventoryCommand>(
        this Either<MappingFault, TInventoryCommand> result
    )
        where TInventoryCommand : class, IInventoryCommand =>
        result.Map(IInventoryCommand (command) => command);

    public static Either<MappingFault, IInventoryEvent> ToBaseEvent<TInventoryEvent>(
        this Either<MappingFault, TInventoryEvent> result
    )
        where TInventoryEvent : class, IInventoryEvent => result.Map(IInventoryEvent (evt) => evt);
}
