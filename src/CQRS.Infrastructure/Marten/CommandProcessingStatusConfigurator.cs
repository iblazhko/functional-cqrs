namespace CQRS.Infrastructure;

using System.Globalization;
using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Configuration;
using LanguageExt;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

public static class CommandProcessingStatusConfigurator
{
    public static IServiceCollection AddMartenDbCommandProcessingStatus(
        this IServiceCollection services,
        MartenDbSettings settings
    )
    {
        services.AddApplicationMartenDb(settings);

        services.AddSingleton<MartenDbCommandProcessingStatusService>();
        services.AddSingleton<ICommandProcessingStatusRecordingService>(sp =>
            sp.GetRequiredService<MartenDbCommandProcessingStatusService>()
        );
        services.AddSingleton<ICommandProcessingStatusQueryService>(sp =>
            sp.GetRequiredService<MartenDbCommandProcessingStatusService>()
        );

        return services;
    }
}

internal sealed class MartenDbCommandProcessingStatusService(IDocumentStore documentStore)
    : ICommandProcessingStatusRecordingService,
        ICommandProcessingStatusQueryService
{
    public async Task RecordCommandProcessingStarted(CommandProcessingRequest request)
    {
        using var session = documentStore.LightweightSession();
        var vm = new CommandProcessingStatusViewModel
        {
            CommandId = request.CommandId,
            CorrelationId = request.CorrelationId,
            CausationId = request.CausationId,
            CommandType = request.CommandType,
            CommandBody = request.CommandBody,
            RequestedAt = request.RequestedAt.ToString("O", CultureInfo.InvariantCulture),
            Status = "Processing",
            Response = string.Empty,
            UpdatedAt = string.Empty,
        };
        session.Store(vm);
        await session.SaveChangesAsync();
    }

    public async Task RecordCommandProcessingCompleted(
        Guid commandId,
        DateTimeOffset completedAt,
        string response = ""
    ) =>
        await UpdateStatus(
            commandId,
            vm =>
                vm with
                {
                    Status = "Completed",
                    Response = response,
                    UpdatedAt = completedAt.ToString("O", CultureInfo.InvariantCulture),
                }
        );

    public async Task RecordCommandProcessingRejected(
        Guid commandId,
        DateTimeOffset rejectedAt,
        string reason
    ) =>
        await UpdateStatus(
            commandId,
            vm =>
                vm with
                {
                    Status = "Rejected",
                    Response = reason,
                    UpdatedAt = rejectedAt.ToString("O", CultureInfo.InvariantCulture),
                }
        );

    public async Task RecordCommandProcessingFailed(
        Guid commandId,
        DateTimeOffset failedAt,
        string failure
    ) =>
        await UpdateStatus(
            commandId,
            vm =>
                vm with
                {
                    Status = "Failed",
                    Response = failure,
                    UpdatedAt = failedAt.ToString("O", CultureInfo.InvariantCulture),
                }
        );

    public async Task<Option<CommandProcessingStatusViewModel>> GetCommandProcessingStatus(
        Guid commandId
    )
    {
        using var session = documentStore.QuerySession();
        var vm = await session.LoadAsync<CommandProcessingStatusViewModel>(commandId);
        return vm is null ? None : Some(vm);
    }

    public async Task<Option<CommandProcessingStatusViewModel>> GetByCorrelationId(
        Guid correlationId
    )
    {
        using var session = documentStore.QuerySession();
        var vm = await session
            .Query<CommandProcessingStatusViewModel>()
            .Where(x => x.CorrelationId == correlationId)
            .FirstOrDefaultAsync();
        return vm is null ? None : Some(vm);
    }

    private async Task UpdateStatus(
        Guid commandId,
        Func<CommandProcessingStatusViewModel, CommandProcessingStatusViewModel> transform
    )
    {
        using var session = documentStore.LightweightSession();
        var existing = await session.LoadAsync<CommandProcessingStatusViewModel>(commandId);
        if (existing is not null)
        {
            session.Store(transform(existing));
            await session.SaveChangesAsync();
        }
    }
}
