namespace CQRS.Infrastructure;

using CQRS.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class EventStoreConfigurator
{
    public static IServiceCollection AddCqrsEventStore(
        this IServiceCollection services,
        MartenDbSettings settings
    )
    {
        services.AddScoped(
            typeof(Ports.EventStore.IEventPublisher<>),
            typeof(Ports.EventStore.NoOpEventPublisher<>)
        );
        services.AddScoped(
            typeof(Ports.EventStore.IEventStore<,,>),
            typeof(Adapters.MartenDbEventStore.MartenDbEventStoreAdapter<,,>)
        );

        services.AddApplicationMartenDb(settings);

        return services;
    }
}
