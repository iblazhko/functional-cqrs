using CQRS.Domain;
using CQRS.Domain.Inventory;
using CQRS.Projections.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using Shouldly;
using static LanguageExt.Prelude;

namespace CQRS.Projections.Tests;

public sealed class InventoryViewModelProjectionTests
{
    private static InventoryViewModel EmptyVm() => new();

    private static InventoryId SomeId() => InventoryId.NewId();

    private static InventoryName SomeName(string value = "Widget") =>
        InventoryName.CreateUnsafe(value);

    // --- InventoryCreated ---

    [Fact]
    public void Apply_InventoryCreated_SetsId()
    {
        var id = SomeId();
        var evt = new InventoryCreated(id, SomeName(), true);

        var vm = InventoryViewModelProjection.Apply(EmptyVm(), evt);

        vm.Id.ShouldBe((string)id);
    }

    [Fact]
    public void Apply_InventoryCreated_SetsName()
    {
        var evt = new InventoryCreated(SomeId(), SomeName("Widget"), true);

        var vm = InventoryViewModelProjection.Apply(EmptyVm(), evt);

        vm.Name.ShouldBe("Widget");
    }

    [Fact]
    public void Apply_InventoryCreated_SetsStockQuantityToZero()
    {
        var evt = new InventoryCreated(SomeId(), SomeName(), true);

        var vm = InventoryViewModelProjection.Apply(EmptyVm(), evt);

        vm.StockQuantity.ShouldBe(0);
    }

    [Fact]
    public void Apply_InventoryCreated_SetsIsActive()
    {
        var evt = new InventoryCreated(SomeId(), SomeName(), true);

        var vm = InventoryViewModelProjection.Apply(EmptyVm(), evt);

        vm.IsActive.ShouldBeTrue();
    }

    // --- InventoryRenamed ---

    [Fact]
    public void Apply_InventoryRenamed_UpdatesName()
    {
        var id = SomeId();
        var initial = EmptyVm() with { Id = (string)id, Name = "OldName" };
        var evt = new InventoryRenamed(id, SomeName("OldName"), SomeName("NewName"));

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.Name.ShouldBe("NewName");
    }

    [Fact]
    public void Apply_InventoryRenamed_PreservesOtherFields()
    {
        var id = SomeId();
        var initial = EmptyVm() with
        {
            Id = (string)id,
            Name = "Old",
            StockQuantity = 10,
            IsActive = true,
        };
        var evt = new InventoryRenamed(id, SomeName("Old"), SomeName("New"));

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.StockQuantity.ShouldBe(10);
        vm.IsActive.ShouldBeTrue();
    }

    // --- ItemsAddedToInventory ---

    [Fact]
    public void Apply_ItemsAddedToInventory_UpdatesStockQuantity()
    {
        var id = SomeId();
        var initial = EmptyVm() with { Id = (string)id, StockQuantity = 5 };
        var evt = new ItemsAddedToInventory(
            id,
            SomeName(),
            PositiveInteger.CreateUnsafe(3),
            Some(PositiveInteger.CreateUnsafe(5)),
            PositiveInteger.CreateUnsafe(8)
        );

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.StockQuantity.ShouldBe(8);
    }

    [Fact]
    public void Apply_ItemsAddedToInventory_FromEmptyStock_SetsNewQuantity()
    {
        var id = SomeId();
        var initial = EmptyVm() with { Id = (string)id, StockQuantity = 0 };
        var evt = new ItemsAddedToInventory(
            id,
            SomeName(),
            PositiveInteger.CreateUnsafe(5),
            None,
            PositiveInteger.CreateUnsafe(5)
        );

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.StockQuantity.ShouldBe(5);
    }

    // --- ItemsRemovedFromInventory ---

    [Fact]
    public void Apply_ItemsRemovedFromInventory_UpdatesStockQuantity()
    {
        var id = SomeId();
        var initial = EmptyVm() with { Id = (string)id, StockQuantity = 10 };
        var evt = new ItemsRemovedFromInventory(
            id,
            SomeName(),
            PositiveInteger.CreateUnsafe(3),
            PositiveInteger.CreateUnsafe(10),
            Some(PositiveInteger.CreateUnsafe(7))
        );

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.StockQuantity.ShouldBe(7);
    }

    [Fact]
    public void Apply_ItemsRemovedFromInventory_ToEmpty_SetsStockQuantityToZero()
    {
        var id = SomeId();
        var initial = EmptyVm() with { Id = (string)id, StockQuantity = 3 };
        var evt = new ItemsRemovedFromInventory(
            id,
            SomeName(),
            PositiveInteger.CreateUnsafe(3),
            PositiveInteger.CreateUnsafe(3),
            None
        );

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.StockQuantity.ShouldBe(0);
    }

    // --- InventoryDeactivated ---

    [Fact]
    public void Apply_InventoryDeactivated_SetsIsActiveFalse()
    {
        var id = SomeId();
        var initial = EmptyVm() with { Id = (string)id, IsActive = true };
        var evt = new InventoryDeactivated(id, SomeName());

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Apply_InventoryDeactivated_PreservesOtherFields()
    {
        var id = SomeId();
        var initial = EmptyVm() with
        {
            Id = (string)id,
            Name = "Widget",
            StockQuantity = 0,
            IsActive = true,
        };
        var evt = new InventoryDeactivated(id, SomeName("Widget"));

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.Name.ShouldBe("Widget");
        vm.StockQuantity.ShouldBe(0);
    }

    // --- No-op events ---

    [Fact]
    public void Apply_ItemWentInStock_DoesNotModifyVm()
    {
        var id = SomeId();
        var initial = EmptyVm() with
        {
            Id = (string)id,
            Name = "Widget",
            StockQuantity = 5,
            IsActive = true,
        };
        var evt = new ItemWentInStock(id, SomeName(), PositiveInteger.CreateUnsafe(5));

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.ShouldBe(initial);
    }

    [Fact]
    public void Apply_ItemWentOutOfStock_DoesNotModifyVm()
    {
        var id = SomeId();
        var initial = EmptyVm() with
        {
            Id = (string)id,
            Name = "Widget",
            StockQuantity = 0,
            IsActive = true,
        };
        var evt = new ItemWentOutOfStock(id, SomeName());

        var vm = InventoryViewModelProjection.Apply(initial, evt);

        vm.ShouldBe(initial);
    }
}
