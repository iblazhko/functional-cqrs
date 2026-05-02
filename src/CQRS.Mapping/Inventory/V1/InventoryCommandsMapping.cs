using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.EntityIds;
using LanguageExt;

namespace CQRS.Mapping.Inventory.V1;

public sealed class InventoryCommandV1Mapper : IInventoryCommandMapper
{
    public IInventoryCommandDto ToCommandDto(IInventoryCommand domain) =>
        domain switch
        {
            CreateInventory x => x.ToDto(),
            RenameInventory x => x.ToDto(),
            AddItemsToInventory x => x.ToDto(),
            RemoveItemsFromInventory x => x.ToDto(),
            DeactivateInventory x => x.ToDto(),
            _ => throw new InvalidOperationException(
                $"Unknown domain command type: {domain.GetType().FullName ?? domain.GetType().Name}"
            ),
        };

    public Either<MappingFault, IInventoryCommand> ToDomainCommand(IInventoryCommandDto dto) =>
        dto switch
        {
            CreateInventoryCommand x => x.ToDomain().ToBaseCommand(),
            RenameInventoryCommand x => x.ToDomain().ToBaseCommand(),
            AddItemsToInventoryCommand x => x.ToDomain().ToBaseCommand(),
            RemoveItemsFromInventoryCommand x => x.ToDomain().ToBaseCommand(),
            DeactivateInventoryCommand x => x.ToDomain().ToBaseCommand(),
            _ => throw new InvalidOperationException(
                $"Unknown command DTO type: {dto.GetType().FullName ?? dto.GetType().Name}"
            ),
        };
}

public static class InventoryCommandsMappingExtensions
{
    public static CreateInventoryCommand ToDto(this CreateInventory domain) =>
        new() { InventoryId = domain.Id, Name = domain.Name };

    public static Either<MappingFault, CreateInventory> ToDomain(this CreateInventoryCommand dto) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.Name)
            select new CreateInventory(InventoryId.Create(entityId), InventoryName.Create(name))
        ).MapValidationFault<CreateInventoryCommand, CreateInventory>();

    public static RenameInventoryCommand ToDto(this RenameInventory domain) =>
        new() { InventoryId = domain.Id, NewName = domain.NewName };

    public static Either<MappingFault, RenameInventory> ToDomain(this RenameInventoryCommand dto) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.NewName)
            select new RenameInventory(InventoryId.Create(entityId), InventoryName.Create(name))
        ).MapValidationFault<RenameInventoryCommand, RenameInventory>();

    public static AddItemsToInventoryCommand ToDto(this AddItemsToInventory domain) =>
        new() { InventoryId = domain.Id, Count = domain.Count };

    public static Either<MappingFault, AddItemsToInventory> ToDomain(
        this AddItemsToInventoryCommand dto
    ) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from count in PositiveInteger.Create(dto.Count)
            select new AddItemsToInventory(InventoryId.Create(entityId), count)
        ).MapValidationFault<AddItemsToInventoryCommand, AddItemsToInventory>();

    public static RemoveItemsFromInventoryCommand ToDto(this RemoveItemsFromInventory domain) =>
        new() { InventoryId = domain.Id, Count = domain.Count };

    public static Either<MappingFault, RemoveItemsFromInventory> ToDomain(
        this RemoveItemsFromInventoryCommand dto
    ) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from count in PositiveInteger.Create(dto.Count)
            select new RemoveItemsFromInventory(InventoryId.Create(entityId), count)
        ).MapValidationFault<RemoveItemsFromInventoryCommand, RemoveItemsFromInventory>();

    public static DeactivateInventoryCommand ToDto(this DeactivateInventory domain) =>
        new() { InventoryId = domain.Id };

    public static Either<MappingFault, DeactivateInventory> ToDomain(
        this DeactivateInventoryCommand dto
    ) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            select new DeactivateInventory(InventoryId.Create(entityId))
        ).MapValidationFault<DeactivateInventoryCommand, DeactivateInventory>();
}
