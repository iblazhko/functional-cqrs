using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.DTO.Inventory.V1;
using CQRS.Mapping.Inventory.V1;
using LanguageExt;
using Shouldly;
using static CQRS.Mapping.Tests.MappingTestSetup;
using static LanguageExt.Prelude;

namespace CQRS.Mapping.Tests;

public sealed class InventoryEventV1Mapper_ToEventDto_Tests
{
    private readonly InventoryEventV1Mapper _mapper = new();

    [Fact]
    public void InventoryCreated_MapsToDto()
    {
        var domain = new InventoryCreated(TestInventoryId, TestInventoryName, true);

        var dto = (InventoryCreatedEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.Name.ShouldBe(ValidInventoryName);
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void InventoryRenamed_MapsToDto()
    {
        var domain = new InventoryRenamed(
            TestInventoryId,
            TestInventoryName,
            TestInventoryName_Updated
        );

        var dto = (InventoryRenamedEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.OldName.ShouldBe(ValidInventoryName);
        dto.NewName.ShouldBe(ValidUpdatedName);
    }

    [Fact]
    public void ItemsAddedToInventory_WithOldStock_MapsToDto()
    {
        var domain = new ItemsAddedToInventory(
            TestInventoryId,
            TestInventoryName,
            Stock(5),
            Some(Stock(3)),
            Stock(8)
        );

        var dto = (ItemsAddedToInventoryEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.AddedCount.ShouldBe(5);
        dto.OldStockQuantity.ShouldBe(3);
        dto.NewStockQuantity.ShouldBe(8);
    }

    [Fact]
    public void ItemsAddedToInventory_WhenOldStockIsNone_DtoHasZero()
    {
        var domain = new ItemsAddedToInventory(
            TestInventoryId,
            TestInventoryName,
            Stock(5),
            None,
            Stock(5)
        );

        var dto = (ItemsAddedToInventoryEvent)_mapper.ToEventDto(domain);

        dto.OldStockQuantity.ShouldBe(0);
        dto.NewStockQuantity.ShouldBe(5);
    }

    [Fact]
    public void ItemsRemovedFromInventory_WithRemainingStock_MapsToDto()
    {
        var domain = new ItemsRemovedFromInventory(
            TestInventoryId,
            TestInventoryName,
            Stock(2),
            Stock(7),
            Some(Stock(5))
        );

        var dto = (ItemsRemovedFromInventoryEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.RemovedCount.ShouldBe(2);
        dto.OldStockQuantity.ShouldBe(7);
        dto.NewStockQuantity.ShouldBe(5);
    }

    [Fact]
    public void ItemsRemovedFromInventory_WhenNewStockIsNone_DtoHasZero()
    {
        var domain = new ItemsRemovedFromInventory(
            TestInventoryId,
            TestInventoryName,
            Stock(5),
            Stock(5),
            None
        );

        var dto = (ItemsRemovedFromInventoryEvent)_mapper.ToEventDto(domain);

        dto.NewStockQuantity.ShouldBe(0);
    }

    [Fact]
    public void ItemWentInStock_MapsToDto()
    {
        var domain = new ItemWentInStock(TestInventoryId, TestInventoryName, Stock(10));

        var dto = (ItemWentInStockEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.Name.ShouldBe(ValidInventoryName);
        dto.StockQuantity.ShouldBe(10);
    }

    [Fact]
    public void ItemWentOutOfStock_MapsToDto()
    {
        var domain = new ItemWentOutOfStock(TestInventoryId, TestInventoryName);

        var dto = (ItemWentOutOfStockEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.Name.ShouldBe(ValidInventoryName);
    }

    [Fact]
    public void InventoryDeactivated_MapsToDto()
    {
        var domain = new InventoryDeactivated(TestInventoryId, TestInventoryName);

        var dto = (InventoryDeactivatedEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe(ValidInventoryIdString);
        dto.Name.ShouldBe(ValidInventoryName);
    }

    [Fact]
    public void UnknownEventType_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() =>
            _mapper.ToEventDto(new UnknownDomainEvent(TestInventoryId))
        );
    }

    private sealed record UnknownDomainEvent(InventoryId Id) : IInventoryEvent;
}

public sealed class InventoryEventV1Mapper_ToDomainEvent_Tests
{
    private readonly InventoryEventV1Mapper _mapper = new();

    [Fact]
    public void InventoryCreatedEvent_MappsToDomain()
    {
        var dto = new InventoryCreatedEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
            IsActive = true,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (InventoryCreated)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
        domain.Name.ToString().ShouldBe(ValidInventoryName);
        domain.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void InventoryRenamedEvent_MappsToDomain()
    {
        var dto = new InventoryRenamedEvent
        {
            InventoryId = ValidInventoryIdString,
            OldName = ValidInventoryName,
            NewName = ValidUpdatedName,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (InventoryRenamed)RightOf(result);
        domain.OldName.ToString().ShouldBe(ValidInventoryName);
        domain.NewName.ToString().ShouldBe(ValidUpdatedName);
    }

    [Fact]
    public void ItemsAddedToInventoryEvent_WithOldStock_MappsToDomain()
    {
        var dto = new ItemsAddedToInventoryEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
            AddedCount = 5,
            OldStockQuantity = 3,
            NewStockQuantity = 8,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (ItemsAddedToInventory)RightOf(result);
        ((int)domain.AddedCount).ShouldBe(5);
        domain.OldStockQuantity.Match(q => (int)q, () => 0).ShouldBe(3);
        ((int)domain.NewStockQuantity).ShouldBe(8);
    }

    [Fact]
    public void ItemsAddedToInventoryEvent_WhenOldStockIsZero_DomainHasNone()
    {
        var dto = new ItemsAddedToInventoryEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
            AddedCount = 5,
            OldStockQuantity = 0,
            NewStockQuantity = 5,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (ItemsAddedToInventory)RightOf(result);
        domain.OldStockQuantity.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void ItemsRemovedFromInventoryEvent_WithRemainingStock_MappsToDomain()
    {
        var dto = new ItemsRemovedFromInventoryEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
            RemovedCount = 2,
            OldStockQuantity = 7,
            NewStockQuantity = 5,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (ItemsRemovedFromInventory)RightOf(result);
        ((int)domain.RemovedCount).ShouldBe(2);
        ((int)domain.OldStockQuantity).ShouldBe(7);
        domain.NewStockQuantity.Match(q => (int)q, () => 0).ShouldBe(5);
    }

    [Fact]
    public void ItemsRemovedFromInventoryEvent_WhenNewStockIsZero_DomainHasNone()
    {
        var dto = new ItemsRemovedFromInventoryEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
            RemovedCount = 5,
            OldStockQuantity = 5,
            NewStockQuantity = 0,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (ItemsRemovedFromInventory)RightOf(result);
        domain.NewStockQuantity.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void ItemWentInStockEvent_MappsToDomain()
    {
        var dto = new ItemWentInStockEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
            StockQuantity = 10,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (ItemWentInStock)RightOf(result);
        ((int)domain.StockQuantity).ShouldBe(10);
    }

    [Fact]
    public void ItemWentOutOfStockEvent_MappsToDomain()
    {
        var dto = new ItemWentOutOfStockEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (ItemWentOutOfStock)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
    }

    [Fact]
    public void InventoryDeactivatedEvent_MappsToDomain()
    {
        var dto = new InventoryDeactivatedEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        var domain = (InventoryDeactivated)RightOf(result);
        domain.Id.ToString().ShouldBe(ValidInventoryIdString);
        domain.Name.ToString().ShouldBe(ValidInventoryName);
    }

    [Fact]
    public void InvalidInventoryId_ReturnsLeft()
    {
        var dto = new InventoryCreatedEvent
        {
            InventoryId = "invalid-id",
            Name = ValidInventoryName,
            IsActive = true,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void EmptyEventName_ReturnsLeft()
    {
        var dto = new InventoryCreatedEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = "",
            IsActive = true,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void InvalidStockQuantity_ReturnsLeft()
    {
        var dto = new ItemWentInStockEvent
        {
            InventoryId = ValidInventoryIdString,
            Name = ValidInventoryName,
            StockQuantity = 0,
        };

        var result = _mapper.ToDomainEvent(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void UnknownDtoType_ThrowsInvalidOperationException()
    {
        Should.Throw<InvalidOperationException>(() => _mapper.ToDomainEvent(new UnknownEventDto()));
    }

    private sealed record UnknownEventDto : IInventoryEventDto;
}
