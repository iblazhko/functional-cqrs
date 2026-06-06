using CQRS.API.Inventory;
using Flurl;
using Flurl.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Shouldly;

namespace CQRS.System.Integration.Tests;

public class CqrsSystem(Uri apiBaseAddress, Uri applicationBaseAddress)
{
    private const string ApiEndpointReadiness = "/health";
    private const string ApiEndpointInventories = "/inventories";
    private const string AppEndpointCommandStatus = "/commands";

    public async Task<bool> IsReady()
    {
        const string healthyStatus = "Healthy";
        try
        {
            var appResponse = await applicationBaseAddress
                .AppendPathSegment(ApiEndpointReadiness)
                .GetStringAsync();

            var apiResponse = await apiBaseAddress
                .AppendPathSegment(ApiEndpointReadiness)
                .GetStringAsync();

            return appResponse?.Contains(healthyStatus, StringComparison.OrdinalIgnoreCase) == true
                && apiResponse?.Contains(healthyStatus, StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> WaitUntilReady()
    {
        var result = await Policy
            .HandleResult(false)
            .WaitAndRetryAsync(
                Backoff.LinearBackoff(
                    TimeSpan.FromSeconds(5),
                    retryCount: 10,
                    factor: 0.5,
                    fastFirst: false
                )
            )
            .ExecuteAndCaptureAsync(IsReady);

        return result.Outcome == OutcomeType.Successful;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class ReadinessResponse
    {
        public string? Status { get; set; }
    }

    private sealed record CommandStatusResponse
    {
        public string Status { get; init; } = string.Empty;
        public string Response { get; init; } = string.Empty;
    }

    public async Task WaitForCommandProcessed(Guid commandId, int timeoutSeconds = 15)
    {
        const int delayMilliseconds = 200;
        var retryCount = timeoutSeconds * 1000 / delayMilliseconds;
        var result = await Policy
            .HandleResult<string?>(s =>
                s is null || (s != "Completed" && s != "Rejected" && s != "Failed")
            )
            .WaitAndRetryAsync(
                Backoff.LinearBackoff(
                    TimeSpan.FromMilliseconds(delayMilliseconds),
                    retryCount: retryCount,
                    factor: 0.1,
                    fastFirst: true
                )
            )
            .ExecuteAndCaptureAsync(async () =>
            {
                var flurlResponse = await applicationBaseAddress
                    .AppendPathSegment(AppEndpointCommandStatus)
                    .AppendPathSegment(commandId)
                    .AppendPathSegment("status")
                    .AllowHttpStatus("404")
                    .GetAsync();

                if (flurlResponse.StatusCode == 404)
                    return null;

                var status = await flurlResponse.GetJsonAsync<CommandStatusResponse>();
                return status?.Status;
            });

        var finalStatus =
            result.Outcome == OutcomeType.Successful ? result.Result : result.FinalHandledResult;

        var failure = finalStatus switch
        {
            null =>
                $"Command {commandId} was never recorded (may not have reached the application host)",
            "Rejected" or "Failed" => $"Command {commandId} ended with status: {finalStatus}",
            "Processing" => $"Command {commandId} did not complete within the timeout period",
            _ => null,
        };

        if (failure is not null)
            throw new InvalidOperationException(failure);
    }

    public async Task<AcceptedResponse> CreateInventory(
        string inventoryName,
        string inventoryId = ""
    )
    {
        var flurlResponse = await apiBaseAddress
            .AppendPathSegment(ApiEndpointInventories)
            .PostJsonAsync(
                new CreateInventoryRequest { InventoryId = inventoryId, Name = inventoryName }
            );

        var response = await flurlResponse.GetJsonAsync<AcceptedResponse>();
        response.ShouldNotBeNull();

        return response;
    }

    public async Task<AcceptedResponse> RenameInventory(string inventoryId, string newName)
    {
        var flurlResponse = await apiBaseAddress
            .AppendPathSegment(ApiEndpointInventories)
            .AppendPathSegment(inventoryId)
            .AppendPathSegment("rename")
            .PutJsonAsync(new RenameInventoryRequest { Name = newName });

        var response = await flurlResponse.GetJsonAsync<AcceptedResponse>();
        response.ShouldNotBeNull();

        return response;
    }

    public async Task<AcceptedResponse> AddItemsToInventory(string inventoryId, int count)
    {
        var flurlResponse = await apiBaseAddress
            .AppendPathSegment(ApiEndpointInventories)
            .AppendPathSegment(inventoryId)
            .AppendPathSegment("add-items")
            .PutJsonAsync(new AddItemsToInventoryRequest { Count = count });

        var response = await flurlResponse.GetJsonAsync<AcceptedResponse>();
        response.ShouldNotBeNull();

        return response;
    }

    public async Task<AcceptedResponse> RemoveItemsFromInventory(string inventoryId, int count)
    {
        var flurlResponse = await apiBaseAddress
            .AppendPathSegment(ApiEndpointInventories)
            .AppendPathSegment(inventoryId)
            .AppendPathSegment("remove-items")
            .PutJsonAsync(new RemoveItemsFromInventoryRequest { Count = count });

        var response = await flurlResponse.GetJsonAsync<AcceptedResponse>();
        response.ShouldNotBeNull();

        return response;
    }

    public async Task<AcceptedResponse> DeactivateInventory(string inventoryId)
    {
        var flurlResponse = await apiBaseAddress
            .AppendPathSegment(ApiEndpointInventories)
            .AppendPathSegment(inventoryId)
            .AppendPathSegment("deactivate")
            .PutJsonAsync(new DeactivateInventoryRequest());

        var response = await flurlResponse.GetJsonAsync<AcceptedResponse>();
        response.ShouldNotBeNull();

        return response;
    }

    public async Task<InventoryResponse?> GetInventory(string inventoryId)
    {
        var flurlResponse = await apiBaseAddress
            .AppendPathSegment(ApiEndpointInventories)
            .AppendPathSegment(inventoryId)
            .AllowHttpStatus("404")
            .GetAsync();

        if (flurlResponse.StatusCode == 404)
            return null;

        var response = await flurlResponse.GetJsonAsync<InventoryResponse>();
        response.ShouldNotBeNull();

        return response;
    }

    public async Task<InventoryResponse> WaitForInventory(
        string inventoryId,
        Func<InventoryResponse, bool>? predicate = null
    )
    {
        var result = await Policy
            .HandleResult<InventoryResponse?>(r =>
                r is null || (predicate is not null && !predicate(r))
            )
            .WaitAndRetryAsync(
                Backoff.LinearBackoff(
                    TimeSpan.FromMilliseconds(500),
                    retryCount: 60,
                    factor: 0.1,
                    fastFirst: true
                )
            )
            .ExecuteAndCaptureAsync(() => GetInventory(inventoryId));

        var inventory =
            result.Outcome == OutcomeType.Successful ? result.Result : result.FinalHandledResult;

        if (inventory is null)
            throw new InvalidOperationException(
                $"Inventory '{inventoryId}' did not appear within timeout"
            );

        if (predicate is not null && !predicate(inventory))
            throw new InvalidOperationException(
                $"Inventory '{inventoryId}' did not reach expected state within timeout"
            );

        return inventory;
    }
}
