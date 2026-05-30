using System.Text;
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

namespace CQRS.CLI.Commands;

[Command("command-status", Description = "Get command processing status")]
public sealed partial class CommandStatusCommand(CqrsCommandStatusApiClient client) : ICommand
{
    [CommandParameter(0, Name = "commandId", Description = "Command ID (GUID)")]
    public Guid CommandId { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var status = await client.GetCommandStatus(CommandId);
        if (status is null)
        {
            await console.Output.WriteLineAsync($"Command {CommandId} not found.");
            return;
        }

        var statusSummary = new StringBuilder();
        statusSummary.AppendLine($"Status:      {status.Status}");
        if (!string.IsNullOrEmpty(status.Response))
            statusSummary.AppendLine($"Response:    {status.Response}");

        statusSummary.AppendLine();
        statusSummary.AppendLine($"CommandId:   {status.CommandId}");
        statusSummary.AppendLine($"CommandType: {status.CommandType}");
        statusSummary.AppendLine($"CommandBody: {status.CommandBody}");
        statusSummary.AppendLine($"RequestedAt: {status.RequestedAt}");
        statusSummary.AppendLine($"UpdatedAt:   {status.UpdatedAt}");

        await console.Output.WriteLineAsync(statusSummary.ToString());
    }
}
