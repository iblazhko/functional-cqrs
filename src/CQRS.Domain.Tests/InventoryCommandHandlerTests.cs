using CQRS.Domain.Inventory;
using static CQRS.Domain.Tests.DomainTestSetup;
using static LanguageExt.Prelude;

namespace CQRS.Domain.Tests;

public sealed class InventoryCommandHandlerTests
{
    private sealed record UnknownCommand(InventoryId Id) : IInventoryCommand;

    [Fact(DisplayName = "CreateInventory command is dispatched to aggregate")]
    public void Handle_CreateInventory_DispatchesToAggregate()
    {
        InventoryCommandHandler
            .Handle(
                TestInventoryState_None,
                new CreateInventory(TestInventoryId, TestInventoryName),
                MoonPhase.NewMoon
            )
            .ShouldProduceEvents([new InventoryCreated(TestInventoryId, TestInventoryName, true)]);
    }

    [Fact(DisplayName = "RenameInventory command is dispatched to aggregate")]
    public void Handle_RenameInventory_DispatchesToAggregate()
    {
        InventoryCommandHandler
            .Handle(
                TestInventoryState_Current,
                new RenameInventory(TestInventoryId, TestInventoryName_Updated),
                MoonPhase.NewMoon
            )
            .ShouldProduceEvents([
                new InventoryRenamed(TestInventoryId, TestInventoryName, TestInventoryName_Updated),
            ]);
    }

    [Fact(DisplayName = "AddItemsToInventory command is dispatched to aggregate")]
    public void Handle_AddItemsToInventory_DispatchesToAggregate()
    {
        var countToAdd = CreateTestStockQuantity(3);
        InventoryCommandHandler
            .Handle(
                TestInventoryState_NoStock,
                new AddItemsToInventory(TestInventoryId, countToAdd),
                MoonPhase.NewMoon
            )
            .ShouldProduceEvents([
                new ItemsAddedToInventory(
                    TestInventoryId,
                    TestInventoryName,
                    countToAdd,
                    None,
                    countToAdd
                ),
                new ItemWentInStock(TestInventoryId, TestInventoryName, countToAdd),
            ]);
    }

    [Fact(DisplayName = "RemoveItemsFromInventory command is dispatched to aggregate")]
    public void Handle_RemoveItemsFromInventory_DispatchesToAggregate()
    {
        var currentStock = CreateTestStockQuantity(5);
        var countToRemove = CreateTestStockQuantity(2);
        var remaining = CreateTestStockQuantity(3);
        InventoryCommandHandler
            .Handle(
                TestInventoryState_Current with
                {
                    Quantity = currentStock,
                },
                new RemoveItemsFromInventory(TestInventoryId, countToRemove),
                MoonPhase.NewMoon
            )
            .ShouldProduceEvents([
                new ItemsRemovedFromInventory(
                    TestInventoryId,
                    TestInventoryName,
                    countToRemove,
                    currentStock,
                    remaining
                ),
            ]);
    }

    [Fact(DisplayName = "DeactivateInventory command is dispatched to aggregate")]
    public void Handle_DeactivateInventory_DispatchesToAggregate()
    {
        InventoryCommandHandler
            .Handle(
                TestInventoryState_NoStock,
                new DeactivateInventory(TestInventoryId),
                MoonPhase.NewMoon
            )
            .ShouldProduceEvents([new InventoryDeactivated(TestInventoryId, TestInventoryName)]);
    }

    [Fact(DisplayName = "Unknown command returns CommandNotSupported error")]
    public void Handle_UnknownCommand_ReturnsCommandNotSupportedError()
    {
        InventoryCommandHandler
            .Handle(
                TestInventoryState_Current,
                new UnknownCommand(TestInventoryId),
                MoonPhase.NewMoon
            )
            .ShouldProduceFailure(
                new InventoryAggregate.Errors.CommandNotSupported(
                    TestInventoryId,
                    typeof(UnknownCommand).FullName!
                )
            );
    }
}
