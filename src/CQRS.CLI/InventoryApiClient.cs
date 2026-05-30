using CliFx;
using Flurl.Http;

namespace CQRS.CLI;

public sealed record InventoryResponse(
    string InventoryId,
    string Name,
    int StockQuantity,
    bool IsActive
);

public sealed record AcceptedResponse(
    string? InventoryId,
    Guid? CommandId,
    Guid? CorrelationId,
    Guid? CausationId
);

public sealed class InventoryApiClient(CliSettings settings)
{
    private string InventoriesUrl => $"{settings.ApiServiceUrl}/inventories";

    public async Task<InventoryResponse?> GetInventory(string id)
    {
        try
        {
            return await $"{InventoriesUrl}/{id}".GetJsonAsync<InventoryResponse>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 404)
        {
            return null;
        }
        catch (FlurlHttpException ex)
        {
            throw new CommandException(ex.InnerException?.Message ?? ex.Message);
        }
    }

    public Task<AcceptedResponse> CreateInventory(string? id, string name) =>
        ExecuteWrite(() =>
            InventoriesUrl.PostJsonAsync(new { InventoryId = id ?? string.Empty, Name = name })
        );

    public Task<AcceptedResponse> RenameInventory(string id, string name) =>
        ExecuteWrite(() => $"{InventoriesUrl}/{id}/rename".PutJsonAsync(new { Name = name }));

    public Task<AcceptedResponse> AddItems(string id, int count) =>
        ExecuteWrite(() => $"{InventoriesUrl}/{id}/add-items".PutJsonAsync(new { Count = count }));

    public Task<AcceptedResponse> RemoveItems(string id, int count) =>
        ExecuteWrite(() =>
            $"{InventoriesUrl}/{id}/remove-items".PutJsonAsync(new { Count = count })
        );

    public Task<AcceptedResponse> Deactivate(string id) =>
        ExecuteWrite(() => $"{InventoriesUrl}/{id}/deactivate".PutJsonAsync(new { }));

    private static async Task<AcceptedResponse> ExecuteWrite(Func<Task<IFlurlResponse>> call)
    {
        try
        {
            var response = await call();
            return await response.GetJsonAsync<AcceptedResponse>();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode == 400)
        {
            var body = await ex.GetResponseStringAsync();
            throw new CommandException($"Request rejected: {body}");
        }
        catch (FlurlHttpException ex)
        {
            throw new CommandException(ex.InnerException?.Message ?? ex.Message);
        }
    }
}
