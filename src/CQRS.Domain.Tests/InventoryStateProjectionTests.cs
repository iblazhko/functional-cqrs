using CQRS.Domain.Inventory;
using Shouldly;
using static CQRS.Domain.Tests.DomainTestSetup;
using static LanguageExt.Prelude;

namespace CQRS.Domain.Tests;

public sealed class InventoryStateProjectionTests
{
    [Fact(DisplayName = $"Apply {nameof(InventoryCreated)} event")]
    public void ApplyInventoryCreatedEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_New,
                new InventoryCreated(TestInventoryId, TestInventoryName, true)
            )
            .ShouldBe(new InventoryState(TestInventoryId, TestInventoryName, None, false, true));
    }

    [Fact(DisplayName = $"Apply {nameof(InventoryDeactivated)} event")]
    public void ApplyInventoryDeactivatedEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_NoStock,
                new InventoryDeactivated(TestInventoryId, TestInventoryName)
            )
            .ShouldBe(new InventoryState(TestInventoryId, TestInventoryName, None, false, false));
    }

    [Fact(DisplayName = $"Apply {nameof(InventoryRenamed)} event")]
    public void ApplyInventoryRenamedEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_Current,
                new InventoryRenamed(TestInventoryId, TestInventoryName, TestInventoryName_Updated)
            )
            .ShouldBe(
                new InventoryState(
                    TestInventoryId,
                    TestInventoryName_Updated,
                    TestInventoryState_Current.Quantity,
                    false,
                    true
                )
            );
    }

    [Fact(DisplayName = $"Apply {nameof(ItemsAddedToInventory)} event")]
    public void ApplyItemsAddedToInventoryEvent()
    {
        var currentStock = CreateTestStockQuantity(5);
        var addedStock = CreateTestStockQuantity(2);
        var newStock = CreateTestStockQuantity(7);
        InventoryStateProjection
            .Apply(
                TestInventoryState_Current with
                {
                    Quantity = currentStock,
                },
                new ItemsAddedToInventory(
                    TestInventoryId,
                    TestInventoryName,
                    addedStock,
                    currentStock,
                    newStock
                )
            )
            .ShouldBe(
                new InventoryState(TestInventoryId, TestInventoryName, newStock, false, true)
            );
    }

    [Fact(DisplayName = $"Apply {nameof(ItemsRemovedFromInventory)} event")]
    public void ApplyItemsRemovedFromInventoryEvent()
    {
        var currentStock = CreateTestStockQuantity(5);
        var removedStock = CreateTestStockQuantity(2);
        var newStock = CreateTestStockQuantity(3);
        InventoryStateProjection
            .Apply(
                TestInventoryState_Current with
                {
                    Quantity = currentStock,
                },
                new ItemsRemovedFromInventory(
                    TestInventoryId,
                    TestInventoryName,
                    removedStock,
                    currentStock,
                    newStock
                )
            )
            .ShouldBe(
                new InventoryState(TestInventoryId, TestInventoryName, newStock, false, true)
            );
    }

    [Fact(DisplayName = $"Apply {nameof(ItemWentInStock)} event (NO-OP)")]
    public void ApplyItemInStockEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_Current,
                new ItemWentInStock(TestInventoryId, TestInventoryName, CreateTestStockQuantity(3))
            )
            .ShouldBe(TestInventoryState_Current);
    }

    [Fact(DisplayName = $"Apply {nameof(ItemWentOutOfStock)} event (NO-OP)")]
    public void ApplyItemWentOutOfStockEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_Current,
                new ItemWentOutOfStock(TestInventoryId, TestInventoryName)
            )
            .ShouldBe(TestInventoryState_Current);
    }

    [Fact(DisplayName = "GetInitialState returns new state with correct id")]
    public void GetInitialState_ReturnsNewStateWithGivenId()
    {
        var state = InventoryStateProjection.GetInitialState(TestInventoryId);

        state.Id.ShouldBe(TestInventoryId);
        state.IsNew.ShouldBeTrue();
        state.Quantity.IsNone.ShouldBeTrue();
    }

    [Fact(DisplayName = "Apply unsupported event type throws NotSupportedException")]
    public void ApplyUnsupportedEvent_ThrowsNotSupportedException()
    {
        Should.Throw<NotSupportedException>(() =>
            InventoryStateProjection.Apply(TestInventoryState_Current, new UnsupportedTestEvent(TestInventoryId))
        );
    }

    private sealed record UnsupportedTestEvent(InventoryId Id) : IInventoryEvent;
}
