using Shouldly;
using CQRS.Domain.Inventory;

namespace CQRS.System.Integration.Tests;

[Collection(DockerComposeCollectionFixture.DockerComposeTestsCollection)]
public class InventoryApiTests(CqrsTestContainersFixture fixture)
{
    // -------------------------------------------------------------------------
    // CreateInventory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateInventory_WithValidRequest_ShouldBeAccepted()
    {
        const string inventoryName = "TEST-123";
        var inventoryId = InventoryId.NewId().ToString();

        var response = await fixture.SUT.CreateInventory(inventoryName, inventoryId);

        response.InventoryId.ShouldBe(inventoryId);
        response.CorrelationId.ShouldNotBeNull();
        response.CausationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateInventory_WithoutInventoryId_ShouldBeAcceptedWithGeneratedId()
    {
        var response = await fixture.SUT.CreateInventory("TEST-AUTO-ID");

        response.InventoryId.ShouldNotBeNullOrWhiteSpace();
        response.CorrelationId.ShouldNotBeNull();
        response.CausationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task  CreateInventory_WithValidRequestAndValidState_ShouldBeProcessed()
    {
        const string inventoryName = "TEST-GET-AFTER-CREATE";
        var inventoryId = InventoryId.NewId().ToString();

        var createResponse = await fixture.SUT.CreateInventory(inventoryName, inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);

        var inventory = await fixture.SUT.WaitForInventory(inventoryId);

        inventory.InventoryId.ShouldBe(inventoryId);
        inventory.Name.ShouldBe(inventoryName);
        inventory.StockQuantity.ShouldBe(0);
        inventory.IsActive.ShouldBeTrue();
    }

    // -------------------------------------------------------------------------
    // RenameInventory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RenameInventory_WithValidRequest_ShouldBeAccepted()
    {
        var inventoryId = InventoryId.NewId().ToString();
        await fixture.SUT.CreateInventory("TEST-RENAME-ORIGINAL", inventoryId);

        var response = await fixture.SUT.RenameInventory(inventoryId, "TEST-RENAME-UPDATED");

        response.InventoryId.ShouldBe(inventoryId);
        response.CorrelationId.ShouldNotBeNull();
        response.CausationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task RenameInventory_WithValidRequestAndValidState_ShouldBeProcessed()
    {
        const string initialName = "TEST-RENAME-BEFORE";
        const string updatedName = "TEST-RENAMED";
        var inventoryId = InventoryId.NewId().ToString();

        var createResponse = await fixture.SUT.CreateInventory(initialName, inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.Name == initialName);

        var renameResponse = await fixture.SUT.RenameInventory(inventoryId, updatedName);
        await fixture.SUT.WaitForCommandProcessed(renameResponse.CommandId!.Value);
        var inventory = await fixture.SUT.WaitForInventory(inventoryId, vm => vm.Name == updatedName);

        inventory.Name.ShouldBe(updatedName);
    }

    // -------------------------------------------------------------------------
    // AddItemsToInventory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddItemsToInventory_WithValidRequest_ShouldBeAccepted()
    {
        var inventoryId = InventoryId.NewId().ToString();
        await fixture.SUT.CreateInventory("TEST-ADD-ITEMS", inventoryId);

        var response = await fixture.SUT.AddItemsToInventory(inventoryId, 10);

        response.InventoryId.ShouldBe(inventoryId);
        response.CorrelationId.ShouldNotBeNull();
        response.CausationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddItemsToInventory_WithValidRequestAndValidState_ShouldBeProcessed()
    {
        const int itemCount = 15;
        var inventoryId = InventoryId.NewId().ToString();

        var createResponse = await fixture.SUT.CreateInventory("TEST-ADD-ITEMS-PROJECTION", inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == 0);

        var addResponse = await fixture.SUT.AddItemsToInventory(inventoryId, itemCount);
        await fixture.SUT.WaitForCommandProcessed(addResponse.CommandId!.Value);
        var inventory = await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == itemCount);

        inventory.StockQuantity.ShouldBe(itemCount);
    }

    // -------------------------------------------------------------------------
    // RemoveItemsFromInventory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveItemsFromInventory_WithValidRequest_ShouldBeAccepted()
    {
        var inventoryId = InventoryId.NewId().ToString();

        var createResponse = await fixture.SUT.CreateInventory("TEST-REMOVE-ITEMS", inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == 0);

        var addResponse = await fixture.SUT.AddItemsToInventory(inventoryId, 20);
        await fixture.SUT.WaitForCommandProcessed(addResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == 20);

        var response = await fixture.SUT.RemoveItemsFromInventory(inventoryId, 5);

        response.InventoryId.ShouldBe(inventoryId);
        response.CorrelationId.ShouldNotBeNull();
        response.CausationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task RemoveItemsFromInventory_WithValidRequestAndValidState_ShouldBeProcessed()
    {
        const int addCount = 20;
        const int removeCount = 7;
        var inventoryId = InventoryId.NewId().ToString();

        var createResponse = await fixture.SUT.CreateInventory("TEST-REMOVE-ITEMS-PROJECTION", inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == 0);

        var addResponse = await fixture.SUT.AddItemsToInventory(inventoryId, addCount);
        await fixture.SUT.WaitForCommandProcessed(addResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == addCount);

        var removeResponse = await fixture.SUT.RemoveItemsFromInventory(inventoryId, removeCount);
        await fixture.SUT.WaitForCommandProcessed(removeResponse.CommandId!.Value);

        var inventory = await fixture.SUT.WaitForInventory(
            inventoryId,
            vm => vm.StockQuantity == addCount - removeCount
        );

        inventory.StockQuantity.ShouldBe(addCount - removeCount);
    }

    // -------------------------------------------------------------------------
    // DeactivateInventory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeactivateInventory_WithValidRequest_ShouldBeAccepted()
    {
        var inventoryId = InventoryId.NewId().ToString();

        var createResponse = await fixture.SUT.CreateInventory("TEST-DEACTIVATE", inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == 0);

        var response = await fixture.SUT.DeactivateInventory(inventoryId);

        response.InventoryId.ShouldBe(inventoryId);
        response.CorrelationId.ShouldNotBeNull();
        response.CausationId.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeactivateInventory_WithValidRequestAndValidState_ShouldBeProcessed()
    {
        var inventoryId = InventoryId.NewId().ToString();

        var createResponse = await fixture.SUT.CreateInventory("TEST-DEACTIVATE-PROJECTION", inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await fixture.SUT.WaitForInventory(inventoryId, vm => vm.StockQuantity == 0);

        var deactivateResponse = await fixture.SUT.DeactivateInventory(inventoryId);
        await fixture.SUT.WaitForCommandProcessed(deactivateResponse.CommandId!.Value);

        var inventory = await fixture.SUT.WaitForInventory(
            inventoryId,
            vm => !vm.IsActive
        );

        inventory.IsActive.ShouldBeFalse();
    }

    // -------------------------------------------------------------------------
    // GetInventory
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetInventory_WhenInventoryDoesNotExist_ShouldReturnNotFound()
    {
        var nonExistentId = InventoryId.NewId().ToString();

        var response = await fixture.SUT.GetInventory(nonExistentId);

        response.ShouldBeNull();
    }
}
