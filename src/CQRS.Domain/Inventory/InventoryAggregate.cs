namespace CQRS.Domain.Inventory;

public static class InventoryAggregate
{
    private static readonly Seq<IInventoryEvent> NoEvents = Empty;

    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> CreateInventory(
        Option<InventoryState> state,
        CreateInventory command
    ) => InvokeIfNew(state, () => new InventoryCreated(command.Id, command.Name, true).ToSeq());

    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> RenameInventory(
        Option<InventoryState> state,
        RenameInventory command
    ) =>
        InvokeIfActive(
            state,
            command.Id,
            existing =>
                existing.Name != command.NewName
                    ? new InventoryRenamed(existing.Id, existing.Name, command.NewName).ToSeq()
                    : NoEvents
        );

    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> AddItemsToInventory(
        Option<InventoryState> state,
        AddItemsToInventory command
    ) =>
        InvokeIfActive(
            state,
            command.Id,
            existing =>
            {
                return ProduceEvents().ToSeq();

                IEnumerable<IInventoryEvent> ProduceEvents()
                {
                    var newQuantity = existing.Quantity + command.Count;

                    yield return new ItemsAddedToInventory(
                        existing.Id,
                        existing.Name,
                        command.Count,
                        existing.Quantity,
                        newQuantity
                    );

                    if (existing.Quantity.IsNone)
                    {
                        yield return new ItemWentInStock(existing.Id, existing.Name, newQuantity);
                    }
                }
            }
        );

    public static Either<
        Errors.IInventoryCommandError,
        Seq<IInventoryEvent>
    > RemoveItemsFromInventory(Option<InventoryState> state, RemoveItemsFromInventory command) =>
        InvokeIfActive(
            state,
            command.Id,
            existing =>
            {
                var quantityInStock = existing.Quantity.Match(quantity => (int)quantity, () => 0);
                var quantityRequested = (int)command.Count;

                if (quantityRequested > quantityInStock)
                    return new Errors.CannotRemoveMoreThanHaveInStock(existing.Id);

                return ProduceEvents().ToSeq();

                IEnumerable<IInventoryEvent> ProduceEvents()
                {
                    var newQuantity = (quantityInStock - quantityRequested) switch
                    {
                        0 => None,
                        var n => Some(PositiveInteger.CreateUnsafe(n)),
                    };

                    yield return new ItemsRemovedFromInventory(
                        existing.Id,
                        existing.Name,
                        command.Count,
                        PositiveInteger.CreateUnsafe(quantityInStock),
                        newQuantity
                    );

                    if (newQuantity.IsNone)
                        yield return new ItemWentOutOfStock(existing.Id, existing.Name);
                }
            }
        );

    // Random business rule: cannot deactivate an inventory when the moon is in full phase
    public static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> DeactivateInventory(
        Option<InventoryState> state,
        DeactivateInventory command,
        MoonPhase moonPhase
    ) =>
        InvokeIfExists(
            state,
            command.Id,
            existing =>
            {
                if (!existing.IsActive)
                    return NoEvents;

                if (!existing.Quantity.IsNone)
                    return new Errors.CannotDeactivateNonEmpty(existing.Id);

                if (moonPhase.IsFullMoon())
                    return new Errors.CannotDeactivateWhenMoonIsFull(existing.Id);

                return new InventoryDeactivated(existing.Id, existing.Name).ToSeq();
            }
        );

    private static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> InvokeIfNew(
        Option<InventoryState> state,
        Func<Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>>> action
    ) =>
        state.Match(Some: existing => new Errors.InventoryAlreadyExists(existing.Id), None: action);

    private static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> InvokeIfExists(
        Option<InventoryState> state,
        InventoryId id,
        Func<InventoryState, Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>>> action
    ) => state.Match(Some: action, None: () => new Errors.InventoryDoesNotExist(id));

    private static Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>> InvokeIfActive(
        Option<InventoryState> state,
        InventoryId id,
        Func<InventoryState, Either<Errors.IInventoryCommandError, Seq<IInventoryEvent>>> action
    ) =>
        InvokeIfExists(
            state,
            id,
            existing =>
                existing.IsActive ? action(existing) : new Errors.CannotChangeInactive(existing.Id)
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

    public static Seq<IInventoryEvent> ToSeq(this IEnumerable<IInventoryEvent> events) =>
        toSeq(events);
}
