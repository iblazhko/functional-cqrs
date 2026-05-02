using CQRS.Application.Inventory;
using CQRS.Domain.Inventory;
using Shouldly;

namespace CQRS.Application.Tests;

public sealed class InventoryEventStreamStateProjectionTests
{
    private readonly InventoryEventStreamStateProjection _projection = new();

    [Fact]
    public void GetInitialState_ReturnsNewState()
    {
        var inventoryId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);

        var state = _projection.GetInitialState(streamId);

        state.IsNew.ShouldBeTrue();
    }

    [Fact]
    public void GetInitialState_StateIdMatchesInventoryIdInStream()
    {
        var inventoryId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);

        var state = _projection.GetInitialState(streamId);

        ((string)state.Id).ShouldBe((string)inventoryId);
    }

    [Fact]
    public void Apply_InventoryCreated_ReturnsNonNewState()
    {
        var inventoryId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);
        var initialState = _projection.GetInitialState(streamId);
        var evt = new InventoryCreated(inventoryId, InventoryName.CreateUnsafe("Widget"), true);

        var newState = _projection.Apply(initialState, evt);

        newState.IsNew.ShouldBeFalse();
    }

    [Fact]
    public void Apply_InventoryCreated_PreservesInventoryName()
    {
        var inventoryId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);
        var initialState = _projection.GetInitialState(streamId);
        var evt = new InventoryCreated(inventoryId, InventoryName.CreateUnsafe("Widget"), true);

        var newState = _projection.Apply(initialState, evt);

        ((string)newState.Name).ShouldBe("Widget");
    }

    [Fact]
    public void Apply_InventoryDeactivated_SetsIsActiveFalse()
    {
        var inventoryId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);
        var createdState = _projection.Apply(
            _projection.GetInitialState(streamId),
            new InventoryCreated(inventoryId, InventoryName.CreateUnsafe("Widget"), true)
        );
        var evt = new InventoryDeactivated(inventoryId, InventoryName.CreateUnsafe("Widget"));

        var newState = _projection.Apply(createdState, evt);

        newState.IsActive.ShouldBeFalse();
    }
}
