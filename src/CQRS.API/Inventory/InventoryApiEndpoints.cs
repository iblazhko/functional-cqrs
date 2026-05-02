using Microsoft.AspNetCore.Mvc;

namespace CQRS.API.Inventory;

public static class InventoryApiEndpoints
{
    public static void AddInventoriesApiEndpoints(
        this WebApplication app,
        string prefix = "inventories"
    )
    {
        app.MapGet(prefix + "/", () => "Hello from CQRS API!");

        app.MapGet(
            prefix + "/{id}",
            async ([FromRoute] string id, IInventoriesApiService apiService) =>
                (await apiService.GetInventory(id)).ToHttpResult()
        );

        app.MapPost(
            prefix + "/",
            async ([FromBody] CreateInventoryRequest request, IInventoriesApiService apiService) =>
                (await apiService.CreateInventory(request)).ToHttpResult()
        );

        app.MapPut(
            prefix + "/{id}/rename",
            async (
                [FromRoute] string id,
                [FromBody] RenameInventoryRequest request,
                IInventoriesApiService apiService
            ) => (await apiService.RenameInventory(id, request)).ToHttpResult()
        );

        app.MapPut(
            prefix + "/{id}/add-items",
            async (
                [FromRoute] string id,
                [FromBody] AddItemsToInventoryRequest request,
                IInventoriesApiService apiService
            ) => (await apiService.AddItemsToInventory(id, request)).ToHttpResult()
        );

        app.MapPut(
            prefix + "/{id}/remove-items",
            async (
                [FromRoute] string id,
                [FromBody] RemoveItemsFromInventoryRequest request,
                IInventoriesApiService apiService
            ) => (await apiService.RemoveItemsFromInventory(id, request)).ToHttpResult()
        );

        app.MapPut(
            prefix + "/{id}/deactivate",
            async (
                [FromRoute] string id,
                [FromBody] DeactivateInventoryRequest request,
                IInventoriesApiService apiService
            ) => (await apiService.DeactivateInventory(id, request)).ToHttpResult()
        );
    }
}
