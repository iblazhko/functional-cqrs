using System.Globalization;
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
        statusSummary.AppendLine(CultureInfo.InvariantCulture, $"Status:      {status.Status}");
        if (!string.IsNullOrEmpty(status.Response))
            statusSummary.AppendLine(
                CultureInfo.InvariantCulture,
                $"Response:    {status.Response}"
            );

        statusSummary.AppendLine();
        statusSummary.AppendLine(CultureInfo.InvariantCulture, $"CommandId:   {status.CommandId}");
        statusSummary.AppendLine(
            CultureInfo.InvariantCulture,
            $"CommandType: {status.CommandType}"
        );
        statusSummary.AppendLine(
            CultureInfo.InvariantCulture,
            $"CommandBody: {status.CommandBody}"
        );
        statusSummary.AppendLine(
            CultureInfo.InvariantCulture,
            $"RequestedAt: {status.RequestedAt}"
        );
        statusSummary.AppendLine(CultureInfo.InvariantCulture, $"UpdatedAt:   {status.UpdatedAt}");

        await console.Output.WriteLineAsync(statusSummary.ToString());
    }
}
