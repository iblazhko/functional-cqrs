using System.Collections.Concurrent;
using System.Globalization;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CQRS.Application.CommandProcessingStatusRecording;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable NotAccessedPositionalProperty.Global

public interface ICommandProcessingStatusRecordingService
{
    Task RecordCommandProcessingStarted(CommandProcessingRequest request);
    Task RecordCommandProcessingCompleted(
        Guid commandId,
        DateTimeOffset completedAt,
        string response = ""
    );
    Task RecordCommandProcessingRejected(Guid commandId, DateTimeOffset rejectedAt, string reason);
    Task RecordCommandProcessingFailed(Guid commandId, DateTimeOffset failedAt, string failure);
}

public sealed record CommandProcessingRequest(
    Guid CommandId,
    Guid CorrelationId,
    Guid CausationId,
    string CommandType,
    string CommandBody,
    DateTimeOffset RequestedAt
);

public sealed record CommandProcessingStatus(
    Guid CommandId,
    Guid CorrelationId,
    Guid CausationId,
    string CommandType,
    string CommandBody,
    DateTimeOffset RequestedAt,
    Status Status,
    string Response
);

public enum Status
{
    Unknown,
    Processing,
    Completed,
    Rejected,
    Failed,
}

public class CommandProcessingStatusRecordingService
    : ICommandProcessingStatusRecordingService,
        ICommandProcessingStatusQueryService
{
    private readonly ConcurrentDictionary<Guid, CommandProcessingStatusViewModel> _byCommandId =
        new();

    private readonly ConcurrentDictionary<Guid, Guid> _correlationToCommandId = new();

    public Task RecordCommandProcessingStarted(CommandProcessingRequest request)
    {
        var vm = new CommandProcessingStatusViewModel
        {
            CommandId = request.CommandId,
            CorrelationId = request.CorrelationId,
            CausationId = request.CausationId,
            CommandType = request.CommandType,
            CommandBody = request.CommandBody,
            RequestedAt = request.RequestedAt.ToString("O", CultureInfo.InvariantCulture),
            Status = nameof(Status.Processing),
            Response = string.Empty,
            UpdatedAt = string.Empty,
        };
        _byCommandId[request.CommandId] = vm;
        _correlationToCommandId[request.CorrelationId] = request.CommandId;
        return Task.CompletedTask;
    }

    public Task RecordCommandProcessingCompleted(
        Guid commandId,
        DateTimeOffset completedAt,
        string response = ""
    )
    {
        Update(commandId, vm => UpdateStatusVM(vm, Status.Completed, response, completedAt));
        return Task.CompletedTask;
    }

    public Task RecordCommandProcessingRejected(
        Guid commandId,
        DateTimeOffset rejectedAt,
        string reason
    )
    {
        Update(commandId, vm => UpdateStatusVM(vm, Status.Rejected, reason, rejectedAt));
        return Task.CompletedTask;
    }

    public Task RecordCommandProcessingFailed(
        Guid commandId,
        DateTimeOffset failedAt,
        string failure
    )
    {
        Update(commandId, vm => UpdateStatusVM(vm, Status.Failed, failure, failedAt));
        return Task.CompletedTask;
    }

    public Task<Option<CommandProcessingStatusViewModel>> GetCommandProcessingStatus(
        Guid commandId
    ) => Task.FromResult(_byCommandId.TryGetValue(commandId, out var vm) ? Some(vm) : None);

    public Task<Option<CommandProcessingStatusViewModel>> GetByCorrelationId(Guid correlationId) =>
        Task.FromResult(
            _correlationToCommandId.TryGetValue(correlationId, out var commandId)
            && _byCommandId.TryGetValue(commandId, out var vm)
                ? Some(vm)
                : None
        );

    private void Update(
        Guid commandId,
        Func<CommandProcessingStatusViewModel, CommandProcessingStatusViewModel> transform
    )
    {
        if (_byCommandId.TryGetValue(commandId, out var existing))
            _byCommandId[commandId] = transform(existing);
    }

    private static CommandProcessingStatusViewModel UpdateStatusVM(
        CommandProcessingStatusViewModel vm,
        Status status,
        string response,
        DateTimeOffset updatedAt
    ) =>
        vm with
        {
            Status = status.ToString(),
            Response = response,
            UpdatedAt = updatedAt.ToString("O", CultureInfo.InvariantCulture),
        };
}
