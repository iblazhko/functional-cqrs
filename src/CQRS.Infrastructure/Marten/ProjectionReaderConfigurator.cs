using CQRS.Adapters.MartenDbEventStore;
using CQRS.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CQRS.Infrastructure;

public static class ProjectionReaderConfigurator
{
    public static IServiceCollection AddCqrsProjectionReader(
        this IServiceCollection services,
        MartenDbSettings settings
    )
    {
        services.AddSingleton(
            typeof(Ports.EventStore.IEventStoreProjectionReader<>),
            typeof(MartenDbProjectionReader<>)
        );

        services.AddApplicationMartenDb(settings);

        return services;
    }
}
