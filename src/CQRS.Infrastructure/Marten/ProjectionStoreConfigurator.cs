namespace CQRS.Infrastructure;

using CQRS.Adapters.MartenDbProjectionStore;
using CQRS.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class ProjectionStoreConfigurator
{
    public static IServiceCollection AddCqrsProjectionStore(
        this IServiceCollection services,
        MartenDbSettings settings
    )
    {
        services.AddSingleton(
            typeof(Ports.ProjectionStore.IProjectionStore<>),
            typeof(MartenDbProjectionStoreAdapter<>)
        );

        services.AddApplicationMartenDb(settings);

        return services;
    }
}
