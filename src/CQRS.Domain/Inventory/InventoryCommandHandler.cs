namespace CQRS.Domain.Inventory;

public static class InventoryCommandHandler
{
    public static Either<
        InventoryAggregate.Errors.IInventoryCommandError,
        Seq<IInventoryEvent>
    > Handle(InventoryState state, IInventoryCommand command, MoonPhase moonPhase) =>
        command switch
        {
            CreateInventory cmd => InventoryAggregate.CreateInventory(state, cmd),
            RenameInventory cmd => InventoryAggregate.RenameInventory(state, cmd),
            AddItemsToInventory cmd => InventoryAggregate.AddItemsToInventory(state, cmd),
            RemoveItemsFromInventory cmd => InventoryAggregate.RemoveItemsFromInventory(state, cmd),
            DeactivateInventory cmd => InventoryAggregate.DeactivateInventory(
                state,
                cmd,
                moonPhase
            ),
            _ => new InventoryAggregate.Errors.CommandNotSupported(
                command.Id,
                command.GetType().FullName ?? command.GetType().Name
            ),
        };
}
