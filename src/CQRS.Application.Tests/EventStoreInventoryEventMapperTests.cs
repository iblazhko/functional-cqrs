using CQRS.Application.Inventory;
using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.DTO.Inventory.V1;
using CQRS.Mapping;
using Shouldly;
using static LanguageExt.Prelude;

namespace CQRS.Application.Tests;

public sealed class EventStoreInventoryEventMapperTests
{
    private readonly EventStoreInventoryEventMapper _mapper = new();

    private static InventoryId SomeId() => InventoryId.NewId();
    private static InventoryName SomeName() => InventoryName.CreateUnsafe("Widget");

    // --- ToEventDto ---

    [Fact]
    public void ToEventDto_InventoryCreated_ReturnsInventoryCreatedEvent()
    {
        var domain = new InventoryCreated(SomeId(), SomeName(), true);

        var dto = _mapper.ToEventDto(domain);

        dto.ShouldBeOfType<InventoryCreatedEvent>();
    }

    [Fact]
    public void ToEventDto_InventoryCreated_PreservesFields()
    {
        var id = SomeId();
        var name = SomeName();
        var domain = new InventoryCreated(id, name, true);

        var dto = (InventoryCreatedEvent)_mapper.ToEventDto(domain);

        dto.InventoryId.ShouldBe((string)id);
        dto.Name.ShouldBe((string)name);
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void ToEventDto_InventoryRenamed_ReturnsInventoryRenamedEvent()
    {
        var domain = new InventoryRenamed(SomeId(), SomeName(), InventoryName.CreateUnsafe("NewWidget"));

        var dto = _mapper.ToEventDto(domain);

        dto.ShouldBeOfType<InventoryRenamedEvent>();
    }

    [Fact]
    public void ToEventDto_ItemsAddedToInventory_ReturnsItemsAddedToInventoryEvent()
    {
        var domain = new ItemsAddedToInventory(
            SomeId(), SomeName(),
            PositiveInteger.CreateUnsafe(5),
            None,
            PositiveInteger.CreateUnsafe(5)
        );

        var dto = _mapper.ToEventDto(domain);

        dto.ShouldBeOfType<ItemsAddedToInventoryEvent>();
    }

    [Fact]
    public void ToEventDto_ItemsRemovedFromInventory_ReturnsItemsRemovedFromInventoryEvent()
    {
        var domain = new ItemsRemovedFromInventory(
            SomeId(), SomeName(),
            PositiveInteger.CreateUnsafe(2),
            PositiveInteger.CreateUnsafe(5),
            Some(PositiveInteger.CreateUnsafe(3))
        );

        var dto = _mapper.ToEventDto(domain);

        dto.ShouldBeOfType<ItemsRemovedFromInventoryEvent>();
    }

    [Fact]
    public void ToEventDto_InventoryDeactivated_ReturnsInventoryDeactivatedEvent()
    {
        var domain = new InventoryDeactivated(SomeId(), SomeName());

        var dto = _mapper.ToEventDto(domain);

        dto.ShouldBeOfType<InventoryDeactivatedEvent>();
    }

    [Fact]
    public void ToEventDto_ItemWentInStock_ReturnsItemWentInStockEvent()
    {
        var domain = new ItemWentInStock(SomeId(), SomeName(), PositiveInteger.CreateUnsafe(1));

        var dto = _mapper.ToEventDto(domain);

        dto.ShouldBeOfType<ItemWentInStockEvent>();
    }

    [Fact]
    public void ToEventDto_ItemWentOutOfStock_ReturnsItemWentOutOfStockEvent()
    {
        var domain = new ItemWentOutOfStock(SomeId(), SomeName());

        var dto = _mapper.ToEventDto(domain);

        dto.ShouldBeOfType<ItemWentOutOfStockEvent>();
    }

    // --- ToDomainEvent ---

    [Fact]
    public void ToDomainEvent_ValidInventoryCreatedEvent_ReturnsRight()
    {
        var id = SomeId();
        var dto = new InventoryCreatedEvent { InventoryId = (string)id, Name = "Widget", IsActive = true };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => null!, Right: s => s).ShouldBeOfType<InventoryCreated>();
    }

    [Fact]
    public void ToDomainEvent_ValidInventoryCreatedEvent_PreservesFields()
    {
        var id = SomeId();
        var dto = new InventoryCreatedEvent { InventoryId = (string)id, Name = "Widget", IsActive = true };

        var domain = (InventoryCreated)_mapper.ToDomainEvent(dto).Match(Left: _ => null!, Right: s => s);

        ((string)domain.Id).ShouldBe((string)id);
        ((string)domain.Name).ShouldBe("Widget");
        domain.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void ToDomainEvent_InvalidInventoryId_ReturnsLeft()
    {
        var dto = new InventoryCreatedEvent { InventoryId = "bad-id", Name = "Widget" };

        var result = _mapper.ToDomainEvent(dto);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ToDomainEvent_ValidInventoryRenamedEvent_ReturnsRight()
    {
        var id = SomeId();
        var dto = new InventoryRenamedEvent { InventoryId = (string)id, OldName = "Old", NewName = "New" };

        var result = _mapper.ToDomainEvent(dto);

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => null!, Right: s => s).ShouldBeOfType<InventoryRenamed>();
    }
}
