using CQRS.Application.Inventory;
using CQRS.Domain.Inventory;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace CQRS.Application.Tests;

public sealed class InventoryEventStreamStateProjectionTests
{
    private readonly InventoryEventStreamStateProjection _projection = new();

    [Fact]
    public void GetInitialState_ReturnsNone()
    {
        var streamId = InventoryEventStreamId.GetStreamId(InventoryId.NewId());

        var state = _projection.GetInitialState(streamId);

        state.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Apply_InventoryCreated_ReturnsSomeState()
    {
        var inventoryId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);
        var initialState = _projection.GetInitialState(streamId);
        var evt = new InventoryCreated(inventoryId, InventoryName.CreateUnsafe("Widget"), true);

        var newState = _projection.Apply(initialState, evt);

        newState.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void Apply_InventoryCreated_PreservesInventoryName()
    {
        var inventoryId = InventoryId.NewId();
        var streamId = InventoryEventStreamId.GetStreamId(inventoryId);
        var initialState = _projection.GetInitialState(streamId);
        var evt = new InventoryCreated(inventoryId, InventoryName.CreateUnsafe("Widget"), true);

        var newState = _projection.Apply(initialState, evt);

        newState.Map(s => (string)s.Name).ShouldBe(Some("Widget"));
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

        newState.Map(s => s.IsActive).ShouldBe(Some(false));
    }
}
