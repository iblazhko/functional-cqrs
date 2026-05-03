using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Application.Inventory;
using Microsoft.Extensions.DependencyInjection;

namespace CQRS.Application;

public static class Module
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITimeProvider, ApplicationTimeProvider>();

        services.AddSingleton<Random>();
        services.AddSingleton<IMoonPhaseService, MoonPhaseService>();

        services.AddSingleton<InventoryEventStreamStateProjection>();
        services.AddSingleton<EventStoreInventoryEventMapper>();
        services.AddScoped<InventoryCommandDtoHandler>();

        return services;
    }

    public static IServiceCollection AddInMemoryCommandProcessingStatus(
        this IServiceCollection services
    )
    {
        services.AddSingleton<CommandProcessingStatusRecordingService>();
        services.AddSingleton<ICommandProcessingStatusRecordingService>(sp =>
            sp.GetRequiredService<CommandProcessingStatusRecordingService>()
        );
        services.AddSingleton<ICommandProcessingStatusQueryService>(sp =>
            sp.GetRequiredService<CommandProcessingStatusRecordingService>()
        );

        return services;
    }
}
