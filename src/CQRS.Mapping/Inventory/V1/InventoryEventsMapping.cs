using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.EntityIds;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CQRS.Mapping.Inventory.V1;

public sealed class InventoryEventV1Mapper : IInventoryEventMapper
{
    public IInventoryEventDto ToEventDto(IInventoryEvent domain) =>
        domain switch
        {
            InventoryCreated x => x.ToDto(),
            InventoryRenamed x => x.ToDto(),
            ItemsAddedToInventory x => x.ToDto(),
            ItemsRemovedFromInventory x => x.ToDto(),
            ItemWentInStock x => x.ToDto(),
            ItemWentOutOfStock x => x.ToDto(),
            InventoryDeactivated x => x.ToDto(),
            _ => throw new InvalidOperationException(
                $"Unknown domain event type: {domain.GetType().FullName ?? domain.GetType().Name}"
            ),
        };

    public Either<MappingFault, IInventoryEvent> ToDomainEvent(IInventoryEventDto dto) =>
        dto switch
        {
            InventoryCreatedEvent x => x.ToDomain().ToBaseEvent(),
            InventoryRenamedEvent x => x.ToDomain().ToBaseEvent(),
            ItemsAddedToInventoryEvent x => x.ToDomain().ToBaseEvent(),
            ItemsRemovedFromInventoryEvent x => x.ToDomain().ToBaseEvent(),
            ItemWentInStockEvent x => x.ToDomain().ToBaseEvent(),
            ItemWentOutOfStockEvent x => x.ToDomain().ToBaseEvent(),
            InventoryDeactivatedEvent x => x.ToDomain().ToBaseEvent(),
            _ => throw new InvalidOperationException(
                $"Unknown event DTO type: {dto.GetType().FullName ?? dto.GetType().Name}"
            ),
        };
}

public static class InventoryEventsMappingExtensions
{
    public static InventoryCreatedEvent ToDto(this InventoryCreated domain) =>
        new()
        {
            InventoryId = domain.Id,
            Name = domain.Name,
            IsActive = domain.IsActive,
        };

    public static Either<MappingFault, InventoryCreated> ToDomain(this InventoryCreatedEvent dto) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.Name)
            select new InventoryCreated(
                InventoryId.Create(entityId),
                InventoryName.Create(name),
                dto.IsActive
            )
        ).MapValidationFault<InventoryCreatedEvent, InventoryCreated>();

    public static InventoryRenamedEvent ToDto(this InventoryRenamed domain) =>
        new()
        {
            InventoryId = domain.Id,
            OldName = domain.OldName,
            NewName = domain.NewName,
        };

    public static Either<MappingFault, InventoryRenamed> ToDomain(this InventoryRenamedEvent dto) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from oldName in MediumString.Create(dto.OldName)
            from newName in MediumString.Create(dto.NewName)
            select new InventoryRenamed(
                InventoryId.Create(entityId),
                InventoryName.Create(oldName),
                InventoryName.Create(newName)
            )
        ).MapValidationFault<InventoryRenamedEvent, InventoryRenamed>();

    public static ItemsAddedToInventoryEvent ToDto(this ItemsAddedToInventory domain) =>
        new()
        {
            InventoryId = domain.Id,
            Name = domain.Name,
            AddedCount = domain.AddedCount,
            OldStockQuantity = domain.OldStockQuantity.Match(some => (int)some, 0),
            NewStockQuantity = domain.NewStockQuantity,
        };

    public static Either<MappingFault, ItemsAddedToInventory> ToDomain(
        this ItemsAddedToInventoryEvent dto
    ) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.Name)
            from addedCount in PositiveInteger.Create(dto.AddedCount)
            from oldStockQuantity in dto.OldStockQuantity == 0
                ? Option<PositiveInteger>.None
                : PositiveInteger.Create(dto.OldStockQuantity).Map(Some)
            from newStockQuantity in PositiveInteger.Create(dto.NewStockQuantity)
            select new ItemsAddedToInventory(
                InventoryId.Create(entityId),
                InventoryName.Create(name),
                addedCount,
                oldStockQuantity,
                newStockQuantity
            )
        ).MapValidationFault<ItemsAddedToInventoryEvent, ItemsAddedToInventory>();

    public static ItemsRemovedFromInventoryEvent ToDto(this ItemsRemovedFromInventory domain) =>
        new()
        {
            InventoryId = domain.Id,
            Name = domain.Name,
            RemovedCount = domain.RemovedCount,
            OldStockQuantity = domain.OldStockQuantity,
            NewStockQuantity = domain.NewStockQuantity.Match(some => (int)some, 0),
        };

    public static Either<MappingFault, ItemsRemovedFromInventory> ToDomain(
        this ItemsRemovedFromInventoryEvent dto
    ) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.Name)
            from removedCount in PositiveInteger.Create(dto.RemovedCount)
            from oldStockQuantity in PositiveInteger.Create(dto.OldStockQuantity)
            from newStockQuantity in dto.NewStockQuantity == 0
                ? Option<PositiveInteger>.None
                : PositiveInteger.Create(dto.NewStockQuantity).Map(Some)
            select new ItemsRemovedFromInventory(
                InventoryId.Create(entityId),
                InventoryName.Create(name),
                removedCount,
                oldStockQuantity,
                newStockQuantity
            )
        ).MapValidationFault<ItemsRemovedFromInventoryEvent, ItemsRemovedFromInventory>();

    public static ItemWentInStockEvent ToDto(this ItemWentInStock domain) =>
        new()
        {
            InventoryId = domain.Id,
            Name = domain.Name,
            StockQuantity = domain.StockQuantity,
        };

    public static Either<MappingFault, ItemWentInStock> ToDomain(this ItemWentInStockEvent dto) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.Name)
            from stockQuantity in PositiveInteger.Create(dto.StockQuantity)
            select new ItemWentInStock(
                InventoryId.Create(entityId),
                InventoryName.Create(name),
                stockQuantity
            )
        ).MapValidationFault<ItemWentInStockEvent, ItemWentInStock>();

    public static ItemWentOutOfStockEvent ToDto(this ItemWentOutOfStock domain) =>
        new() { InventoryId = domain.Id, Name = domain.Name };

    public static Either<MappingFault, ItemWentOutOfStock> ToDomain(
        this ItemWentOutOfStockEvent dto
    ) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.Name)
            select new ItemWentOutOfStock(InventoryId.Create(entityId), InventoryName.Create(name))
        ).MapValidationFault<ItemWentOutOfStockEvent, ItemWentOutOfStock>();

    public static InventoryDeactivatedEvent ToDto(this InventoryDeactivated domain) =>
        new() { InventoryId = domain.Id, Name = domain.Name };

    public static Either<MappingFault, InventoryDeactivated> ToDomain(
        this InventoryDeactivatedEvent dto
    ) =>
        (
            from entityId in EntityId.Create(dto.InventoryId)
            from name in MediumString.Create(dto.Name)
            select new InventoryDeactivated(
                InventoryId.Create(entityId),
                InventoryName.Create(name)
            )
        ).MapValidationFault<InventoryDeactivatedEvent, InventoryDeactivated>();
}
