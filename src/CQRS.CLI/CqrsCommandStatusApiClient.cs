using CliFx;
using CliFx.Infrastructure;
using Flurl.Http;

namespace CQRS.CLI;

public sealed record CommandStatusResponse(
    Guid CommandId,
    Guid CorrelationId,
    Guid CausationId,
    string CommandType,
    string CommandBody,
    string RequestedAt,
    string Status,
    string Response,
    string UpdatedAt
);

public sealed class CqrsCommandStatusApiClient(CliSettings settings)
{
    private string CommandsUrl => $"{settings.AppServiceUrl}/commands";

    public async Task<CommandStatusResponse?> GetCommandStatus(Guid commandId)
    {
        try
        {
            return await $"{CommandsUrl}/{commandId}/status".GetJsonAsync<CommandStatusResponse>();
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

    public async Task WaitForCompletion(Guid commandId, IConsole console)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        var delayMs = 250;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(delayMs);
            delayMs = Math.Min((int)(delayMs * 1.5), 2000);

            var status = await GetCommandStatus(commandId);
            if (string.IsNullOrWhiteSpace(status?.Status))
                continue;

            switch (status.Status)
            {
                case "Completed":
                    await console.Output.WriteLineAsync($"Completed. (CommandId: {commandId})");
                    return;
                case "Rejected":
                    await console.Output.WriteLineAsync(
                        $"Rejected: {status.Response}. (CommandId: {commandId})"
                    );
                    return;
                case "Failed":
                    await console.Output.WriteLineAsync(
                        $"Failed: {status.Response}. (CommandId: {commandId})"
                    );
                    return;
            }
        }

        await console.Output.WriteLineAsync($"Accepted. CommandId: {commandId}");
    }
}
