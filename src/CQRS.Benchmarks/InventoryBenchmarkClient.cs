using System.Diagnostics;
using Flurl.Http;

namespace CQRS.Benchmarks;

internal sealed record AcceptedResponse(
    string? InventoryId,
    Guid? CommandId,
    Guid? CorrelationId,
    Guid? CausationId
);

internal sealed record CommandStatusResponse(string Status, string Response);

public sealed record CommandOutcome(string Status, long ElapsedMs, string? InventoryId = null);

public sealed class InventoryBenchmarkClient(BenchmarkSettings settings)
{
    private string InventoriesUrl => $"{settings.ApiServiceUrl}/inventories";
    private string CommandsUrl => $"{settings.AppServiceUrl}/commands";

    public Task<CommandOutcome> CreateInventory(string name) =>
        ExecuteCommand(
            () => InventoriesUrl.PostJsonAsync(new { InventoryId = string.Empty, Name = name }),
            accepted => accepted.InventoryId
        );

    public Task<CommandOutcome> RenameInventory(string id, string name) =>
        ExecuteCommand(() =>
            $"{InventoriesUrl}/{id}/rename".PutJsonAsync(new { Name = name })
        );

    public Task<CommandOutcome> AddItems(string id, int count) =>
        ExecuteCommand(() =>
            $"{InventoriesUrl}/{id}/add-items".PutJsonAsync(new { Count = count })
        );

    public Task<CommandOutcome> RemoveItems(string id, int count) =>
        ExecuteCommand(() =>
            $"{InventoriesUrl}/{id}/remove-items".PutJsonAsync(new { Count = count })
        );

    public Task<CommandOutcome> Deactivate(string id) =>
        ExecuteCommand(() =>
            $"{InventoriesUrl}/{id}/deactivate".PutJsonAsync(new { })
        );

    private async Task<CommandOutcome> ExecuteCommand(
        Func<Task<IFlurlResponse>> send,
        Func<AcceptedResponse, string?>? extractInventoryId = null
    )
    {
        var sw = Stopwatch.StartNew();
        try
        {
            IFlurlResponse httpResponse;
            try
            {
                httpResponse = await send();
            }
            catch (FlurlHttpException ex) when (ex.StatusCode == 400)
            {
                return new CommandOutcome("Rejected", sw.ElapsedMilliseconds);
            }

            var accepted = await httpResponse.GetJsonAsync<AcceptedResponse>();
            if (accepted?.CommandId is null)
                return new CommandOutcome("Error", sw.ElapsedMilliseconds);

            var inventoryId = extractInventoryId?.Invoke(accepted);
            var status = await PollStatus(accepted.CommandId.Value);
            return new CommandOutcome(status, sw.ElapsedMilliseconds, inventoryId);
        }
        catch (Exception)
        {
            return new CommandOutcome("Error", sw.ElapsedMilliseconds);
        }
    }

    private async Task<string> PollStatus(Guid commandId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(settings.CommandTimeoutSeconds);
        var delayMs = 100;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(delayMs);
            delayMs = Math.Min((int)(delayMs * 1.5), 2000);

            try
            {
                var flurlResponse = await $"{CommandsUrl}/{commandId}/status"
                    .AllowHttpStatus("404")
                    .GetAsync();

                if (flurlResponse.StatusCode == 404)
                    continue;

                var statusResponse = await flurlResponse.GetJsonAsync<CommandStatusResponse>();
                if (statusResponse?.Status is "Completed" or "Rejected" or "Failed")
                    return statusResponse.Status;
            }
            catch
            {
                // keep retrying until timeout
            }
        }

        return "Timeout";
    }
}
