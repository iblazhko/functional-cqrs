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
                TestInventoryState_None,
                new InventoryCreated(TestInventoryId, TestInventoryName, true)
            )
            .ShouldBe(Some(new InventoryState(TestInventoryId, TestInventoryName, None, true)));
    }

    [Fact(DisplayName = $"Apply {nameof(InventoryDeactivated)} event")]
    public void ApplyInventoryDeactivatedEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_NoStock,
                new InventoryDeactivated(TestInventoryId, TestInventoryName)
            )
            .ShouldBe(Some(new InventoryState(TestInventoryId, TestInventoryName, None, false)));
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
                Some(
                    new InventoryState(
                        TestInventoryId,
                        TestInventoryName_Updated,
                        TestInventoryState_Current.Quantity,
                        true
                    )
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
            .ShouldBe(Some(new InventoryState(TestInventoryId, TestInventoryName, newStock, true)));
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
            .ShouldBe(Some(new InventoryState(TestInventoryId, TestInventoryName, newStock, true)));
    }

    [Fact(DisplayName = $"Apply {nameof(ItemWentInStock)} event (NO-OP)")]
    public void ApplyItemInStockEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_Current,
                new ItemWentInStock(TestInventoryId, TestInventoryName, CreateTestStockQuantity(3))
            )
            .ShouldBe(Some(TestInventoryState_Current));
    }

    [Fact(DisplayName = $"Apply {nameof(ItemWentOutOfStock)} event (NO-OP)")]
    public void ApplyItemWentOutOfStockEvent()
    {
        InventoryStateProjection
            .Apply(
                TestInventoryState_Current,
                new ItemWentOutOfStock(TestInventoryId, TestInventoryName)
            )
            .ShouldBe(Some(TestInventoryState_Current));
    }

    [Fact(DisplayName = "GetInitialState returns None (non-existent inventory)")]
    public void GetInitialState_ReturnsNone()
    {
        InventoryStateProjection.GetInitialState().IsNone.ShouldBeTrue();
    }

    [Fact(DisplayName = "Apply unsupported event type throws NotSupportedException")]
    public void ApplyUnsupportedEvent_ThrowsNotSupportedException()
    {
        Should.Throw<NotSupportedException>(() =>
            InventoryStateProjection.Apply(
                TestInventoryState_Current,
                new UnsupportedTestEvent(TestInventoryId)
            )
        );
    }

    [Fact(DisplayName = "Apply non-creation event to None throws NotSupportedException")]
    public void ApplyEventBeforeCreated_ThrowsNotSupportedException()
    {
        Should.Throw<NotSupportedException>(() =>
            InventoryStateProjection.Apply(
                TestInventoryState_None,
                new InventoryRenamed(TestInventoryId, TestInventoryName, TestInventoryName_Updated)
            )
        );
    }

    private sealed record UnsupportedTestEvent(InventoryId Id) : IInventoryEvent;
}
