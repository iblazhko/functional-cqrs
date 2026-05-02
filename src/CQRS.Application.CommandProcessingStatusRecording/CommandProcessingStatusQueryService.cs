using LanguageExt;

namespace CQRS.Application.CommandProcessingStatusRecording;

public interface ICommandProcessingStatusQueryService
{
    Task<Option<CommandProcessingStatusViewModel>> GetCommandProcessingStatus(Guid commandId);
    Task<Option<CommandProcessingStatusViewModel>> GetByCorrelationId(Guid correlationId);
}

public sealed record CommandProcessingStatusViewModel
{
    public Guid CommandId { get; init; } = Guid.Empty;
    public Guid CorrelationId { get; init; } = Guid.Empty;
    public Guid CausationId { get; init; } = Guid.Empty;
    public string CommandType { get; init; } = string.Empty;
    public string CommandBody { get; init; } = string.Empty;
    public string RequestedAt { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Response { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
}
