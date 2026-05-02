namespace CQRS.Domain.Inventory;

public static class InventoryAggregate
{
    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> CreateInventory(
        InventoryState state,
        CreateInventory command
    ) => InvokeIfNew(state, () => new InventoryCreated(command.Id, command.Name, true).ToSeq());

    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> RenameInventory(
        InventoryState state,
        RenameInventory command
    ) =>
        InvokeIfActive(
            state,
            () =>
                state.Name != command.NewName
                    ? new InventoryRenamed(state.Id, state.Name, command.NewName).ToSeq()
                    : Empty
        );

    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> AddItemsToInventory(
        InventoryState state,
        AddItemsToInventory command
    ) =>
        InvokeIfActive(
            state,
            () =>
            {
                return new Seq<IInventoryEvent>(ProduceEvents());

                IEnumerable<IInventoryEvent> ProduceEvents()
                {
                    var newQuantity = state.Quantity + command.Count;

                    yield return
                        new ItemsAddedToInventory(
                            state.Id,
                            state.Name,
                            command.Count,
                            state.Quantity,
                            newQuantity
                        );

                    if (state.Quantity.IsNone)
                    {
                        yield return new ItemWentInStock(state.Id, state.Name, newQuantity);
                    }
                }
            }
        );

    public static Either<
        Errors.IInventoryCommandError,
        Seq<IInventoryEvent>
    > RemoveItemsFromInventory(InventoryState state, RemoveItemsFromInventory command) =>
        InvokeIfActive(
            state,
            () =>
            {
                var quantityInStock = state.Quantity.Match(quantity => (int)quantity, () => 0);
                var quantityRequested = (int)command.Count;

                if (quantityRequested > quantityInStock)
                    return new Errors.CannotRemoveMoreThanHaveInStock(state.Id);

                return new Seq<IInventoryEvent>(ProduceEvents());

                IEnumerable<IInventoryEvent> ProduceEvents()
                {
                    var newQuantity = (quantityInStock - quantityRequested) switch
                    {
                        0 => None,
                        var n => Some(PositiveInteger.CreateUnsafe(n)),
                    };

                    yield return
                        new ItemsRemovedFromInventory(
                            state.Id,
                            state.Name,
                            command.Count,
                            PositiveInteger.CreateUnsafe(quantityInStock),
                            newQuantity
                        );

                    if (newQuantity.IsNone)
                    {
                        yield return new ItemWentOutOfStock(state.Id, state.Name);
                    }
                }
            }
        );

    // Random business rule: cannot deactivate an inventory when the moon is in full phase
    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> DeactivateInventory(
        InventoryState state,
        DeactivateInventory _,
        MoonPhase moonPhase
    ) =>
        InvokeIfExists(
            state,
            () =>
            {
                if (!state.IsActive)
                    return LanguageExt.Seq.empty<IInventoryEvent>();

                if (!state.Quantity.IsNone)
                    return new Errors.CannotDeactivateNonEmpty(state.Id);

                if (moonPhase.IsFullMoon())
                    return new Errors.CannotDeactivateWhenMoonIsFull(state.Id);

                return new InventoryDeactivated(state.Id, state.Name).ToSeq();
            }
        );

    private static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> InvokeIfNew(
        InventoryState state,
        Func<Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>>> action
    ) => state.IsNew ? action() : new Errors.InventoryAlreadyExists(state.Id);

    private static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> InvokeIfExists(
        InventoryState state,
        Func<Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>>> action
    ) => state.IsNew ? new Errors.InventoryDoesNotExist(state.Id) : action();

    private static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> InvokeIfActive(
        InventoryState state,
        Func<Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>>> action
    ) =>
        InvokeIfExists(
            state,
            () => state.IsActive ? action() : new Errors.CannotChangeInactive(state.Id)
        );

    public static class Errors
    {
        public interface IInventoryCommandError
        {
            InventoryId Id { get; }
        }

        // ReSharper disable NotAccessedPositionalProperty.Global
        public sealed record InventoryDoesNotExist(InventoryId Id) : IInventoryCommandError;

        public sealed record InventoryAlreadyExists(InventoryId Id) : IInventoryCommandError;

        public sealed record CannotChangeInactive(InventoryId Id) : IInventoryCommandError;

        public sealed record CannotDeactivateNonEmpty(InventoryId Id) : IInventoryCommandError;

        public sealed record CannotDeactivateWhenMoonIsFull(InventoryId Id)
            : IInventoryCommandError;

        public sealed record CannotRemoveMoreThanHaveInStock(InventoryId Id)
            : IInventoryCommandError;

        public sealed record CommandNotSupported(InventoryId Id, string Command)
            : IInventoryCommandError;
        // ReSharper restore NotAccessedPositionalProperty.Global
    }
}

public static class InventoryEventExtensions
{
    public static Seq<IInventoryEvent> ToSeq(this IInventoryEvent evt) => new([evt]);
}
