using CQRS.Domain.Inventory;
using static CQRS.Domain.Tests.DomainTestSetup;
using static LanguageExt.Prelude;

namespace CQRS.Domain.Tests;

// The write-side state passed to the aggregate is an `Option<InventoryState>`:
// `None` means there are no events for the entity (typically no event stream at all),
// so the inventory "does not exist" yet, while `Some` carries the reconstructed state.
//
// Technically a persisted event stream could be empty (possible in some event stores),
// and as far as this application is concerned the end result is the same - there are
// no events to apply to the projection, so the aggregate receives `None`. This is
// semantically almost the same as having no stream at all, and can only happen as a
// result of an internal error in an EventStream adapter implementation.

public sealed class InventoryAggregateTests
{
    [Fact(DisplayName = "\u2705 New inventory can be created")]
    public void CreateInventory_WithNewState_ShouldProduceInventoryCreatedEvent()
    {
        InventoryAggregate
            .CreateInventory(
                TestInventoryState_None,
                new CreateInventory(TestInventoryId, TestInventoryName)
            )
            .ShouldProduceEvents([new InventoryCreated(TestInventoryId, TestInventoryName, true)]);
    }

    [Fact(
        DisplayName = "\u26d4 Request to create inventory that already exists should be rejected"
    )]
    public void CreateInventory_WithExistingState_ShouldProduceInventoryAlreadyExistsFailure()
    {
        InventoryAggregate
            .CreateInventory(
                TestInventoryState_Current,
                new CreateInventory(TestInventoryId, TestInventoryName)
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.InventoryAlreadyExists(TestInventoryId)
            );

        InventoryAggregate
            .CreateInventory(
                TestInventoryState_Deactivated,
                new CreateInventory(TestInventoryId, TestInventoryName)
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.InventoryAlreadyExists(TestInventoryId)
            );
    }

    [Fact(DisplayName = "\u2705 Existing active inventory can be renamed")]
    public void RenameInventory_WithExistingState_ShouldProduceInventoryRenamedEvent()
    {
        InventoryAggregate
            .RenameInventory(
                TestInventoryState_Current,
                new RenameInventory(TestInventoryId, TestInventoryName_Updated)
            )
            .ShouldProduceEvents([
                new InventoryRenamed(TestInventoryId, TestInventoryName, TestInventoryName_Updated),
            ]);
    }

    [Fact(
        DisplayName = "\u26d4 Request to rename inventory that does not exist should be rejected"
    )]
    public void RenameInventory_WithNewState_ShouldProduceInventoryDoesNotExistFailure()
    {
        InventoryAggregate
            .RenameInventory(
                TestInventoryState_None,
                new RenameInventory(TestInventoryId, TestInventoryName_Updated)
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.InventoryDoesNotExist(TestInventoryId)
            );
    }

    [Fact(DisplayName = "\u26d4 Request to rename deactivated inventory should be rejected")]
    public void RenameInventory_WithDeactivatedState_ShouldProduceCannotChangeInactiveFailure()
    {
        InventoryAggregate
            .RenameInventory(
                TestInventoryState_Deactivated,
                new RenameInventory(TestInventoryId, TestInventoryName_Updated)
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.CannotChangeInactive(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u23f9\ufe0f Request to rename inventory with no changes in the name should not produce new events"
    )]
    public void RenameInventory_WithNoNameChanges_ShouldNotProduceEvents()
    {
        InventoryAggregate
            .RenameInventory(
                TestInventoryState_Current,
                new RenameInventory(TestInventoryId, TestInventoryName)
            )
            .ShouldProduceEvents([]);
    }

    [Fact(DisplayName = "\u2705 Items can be added to an existing active inventory")]
    public void AddItemsToInventory_WithExistingState_ShouldProduceAddItemsToInventoryEvent()
    {
        var countToAdd = CreateTestStockQuantity(2);
        InventoryAggregate
            .AddItemsToInventory(
                TestInventoryState_Current with
                {
                    Quantity = CreateTestStockQuantity(5),
                },
                new AddItemsToInventory(TestInventoryId, countToAdd)
            )
            .ShouldProduceEvents([
                new ItemsAddedToInventory(
                    TestInventoryId,
                    TestInventoryName,
                    countToAdd,
                    TestInventoryState_Current.Quantity,
                    CreateTestStockQuantity(7)
                ),
            ]);
    }

    [Fact(
        DisplayName = "\u26d4 Request to add items to inventory that does not exist should be rejected"
    )]
    public void AddItemsToInventory_WithNewState_ShouldProduceInventoryDoesNotExistFailure()
    {
        InventoryAggregate
            .AddItemsToInventory(
                TestInventoryState_None,
                new AddItemsToInventory(TestInventoryId, CreateTestStockQuantity(2))
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.InventoryDoesNotExist(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u26d4 Request to add items to a deactivated inventory should be rejected"
    )]
    public void AddItemsToInventory_WithDeactivatedState_ShouldProduceCannotChangeInactiveFailure()
    {
        InventoryAggregate
            .AddItemsToInventory(
                TestInventoryState_Deactivated,
                new AddItemsToInventory(TestInventoryId, CreateTestStockQuantity(2))
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.CannotChangeInactive(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u2705\u2714\ufe0f Adding items to an inventory with no items in stock should produce InStock event"
    )]
    public void AddItemsToInventory_WithExistingStateAndNoItemsInStock_ShouldProduceItemInStockEvent()
    {
        var countToAdd = CreateTestStockQuantity(2);
        InventoryAggregate
            .AddItemsToInventory(
                TestInventoryState_NoStock,
                new AddItemsToInventory(TestInventoryId, countToAdd)
            )
            .ShouldProduceEvents([
                new ItemsAddedToInventory(
                    TestInventoryId,
                    TestInventoryName,
                    countToAdd,
                    TestInventoryState_NoStock.Quantity,
                    countToAdd
                ),
                new ItemWentInStock(TestInventoryId, TestInventoryName, countToAdd),
            ]);
    }

    [Fact(
        DisplayName = "\u2705 Items can be removed from an existing active inventory with items in stock"
    )]
    public void RemoveItemsFromInventory_WithExistingState_ShouldProduceItemsRemovedFromInventoryEvent()
    {
        var currentStockCount = CreateTestStockQuantity(5);
        var countToRemove = CreateTestStockQuantity(2);
        var expectedRemainingCount = CreateTestStockQuantity(3);

        InventoryAggregate
            .RemoveItemsFromInventory(
                TestInventoryState_Current with
                {
                    Quantity = currentStockCount,
                },
                new RemoveItemsFromInventory(TestInventoryId, countToRemove)
            )
            .ShouldProduceEvents([
                new ItemsRemovedFromInventory(
                    TestInventoryId,
                    TestInventoryName,
                    countToRemove,
                    currentStockCount,
                    expectedRemainingCount
                ),
            ]);
    }

    [Fact(
        DisplayName = "\u26d4 Request to remove items from inventory that does not exist should be rejected"
    )]
    public void RemoveItemsFromInventory_WithNewState_ShouldProduceInventoryDoesNotExistFailure()
    {
        InventoryAggregate
            .RemoveItemsFromInventory(
                TestInventoryState_None,
                new RemoveItemsFromInventory(TestInventoryId, CreateTestStockQuantity(2))
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.InventoryDoesNotExist(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u26d4 Request to remove items from deactivated inventory should be rejected"
    )]
    public void RemoveItemsFromInventory_WithDeactivatedState_ShouldProduceCannotChangeInactiveFailure()
    {
        InventoryAggregate
            .RemoveItemsFromInventory(
                TestInventoryState_Deactivated,
                new RemoveItemsFromInventory(TestInventoryId, CreateTestStockQuantity(2))
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.CannotChangeInactive(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u26d4 Request to remove more items than currently available should be rejected"
    )]
    public void RemoveItemsFromInventory_WithMoreItemsRequestedThanHaveInStock_ShouldProduceCannotRemoveMoreThanHaveInStockFailure()
    {
        InventoryAggregate
            .RemoveItemsFromInventory(
                TestInventoryState_Current with
                {
                    Quantity = CreateTestStockQuantity(2),
                },
                new RemoveItemsFromInventory(TestInventoryId, CreateTestStockQuantity(3))
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.CannotRemoveMoreThanHaveInStock(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u2705\u2714\ufe0f Removing all available items from inventory should produce WentOutOfStock event"
    )]
    public void RemoveItemsFromInventory_WithAllAvailableItemsToBeRemoved_ShouldProduceItemWentOutOfStockEvent()
    {
        var currentStockCount = CreateTestStockQuantity(5);
        InventoryAggregate
            .RemoveItemsFromInventory(
                TestInventoryState_Current with
                {
                    Quantity = currentStockCount,
                },
                new RemoveItemsFromInventory(TestInventoryId, currentStockCount)
            )
            .ShouldProduceEvents([
                new ItemsRemovedFromInventory(
                    TestInventoryId,
                    TestInventoryName,
                    currentStockCount,
                    currentStockCount,
                    None
                ),
                new ItemWentOutOfStock(TestInventoryId, TestInventoryName),
            ]);
    }

    [Fact(
        DisplayName = "\u2705 Existing active inventory with no items in stock can be deactivated"
    )]
    public void DeactivateInventory_WithExistingStateAndNoStock_ShouldProduceInventoryDeactivatedEvent()
    {
        InventoryAggregate
            .DeactivateInventory(
                TestInventoryState_NoStock,
                new DeactivateInventory(TestInventoryId),
                MoonPhase.NewMoon
            )
            .ShouldProduceEvents([new InventoryDeactivated(TestInventoryId, TestInventoryName)]);
    }

    [Fact(
        DisplayName = "\u26d4 Request to deactivate active inventory with items in stock should be rejected"
    )]
    public void DeactivateInventory_WithExistingStateAndStockAvailable_ShouldProduceCannotDeactivateNonEmptyFailure()
    {
        InventoryAggregate
            .DeactivateInventory(
                TestInventoryState_Current,
                new DeactivateInventory(TestInventoryId),
                MoonPhase.NewMoon
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.CannotDeactivateNonEmpty(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u26d4 Request to deactivate inventory that does not exist should be rejected"
    )]
    public void DeactivateInventory_WithNewState_ShouldProduceInventoryDoesNotExistFailure()
    {
        InventoryAggregate
            .DeactivateInventory(
                TestInventoryState_None,
                new DeactivateInventory(TestInventoryId),
                MoonPhase.NewMoon
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.InventoryDoesNotExist(TestInventoryId)
            );
    }

    [Fact(
        DisplayName = "\u23f9\ufe0f Request to deactivate already inactive inventory should not produce new events"
    )]
    public void DeactivateInventory_WithDeactivatedState_ShouldNotProduceEvents()
    {
        InventoryAggregate
            .DeactivateInventory(
                TestInventoryState_Deactivated,
                new DeactivateInventory(TestInventoryId),
                MoonPhase.NewMoon
            )
            .ShouldProduceEvents([]);
    }

    [Fact(
        DisplayName = "\u26d4\ud83e\udd2a Request to deactivate inventory should be rejected when the moon is full"
    )]
    public void DeactivateInventory_WithMoonInFullPhase_ShouldProduceCannotDeactivateWhenMoonIsFullFailure()
    {
        InventoryAggregate
            .DeactivateInventory(
                TestInventoryState_NoStock,
                new DeactivateInventory(TestInventoryId),
                MoonPhase.FullMoon
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.CannotDeactivateWhenMoonIsFull(TestInventoryId)
            );
    }
}
