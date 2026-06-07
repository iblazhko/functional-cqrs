namespace CQRS.Infrastructure;

using System;
using CQRS.Adapters.WolverineMessageBus;
using CQRS.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.RabbitMQ;
using PortMessageBus = CQRS.Ports.MessageBus.IMessageBus;

public static class MessageBusConfigurator
{
    public static IServiceCollection AddCqrsMessageBus(
        this IServiceCollection services,
        HostEndpointsRegistration endpointsRegistration,
        MessageBusSettings settings
    )
    {
        services.AddScoped<PortMessageBus, WolverineMessageBusAdapter>();

        services.AddWolverine(opts =>
        {
            opts.UseRabbitMq(new Uri(settings.RabbitMq.GetAmqpUrl())).AutoProvision();

            endpointsRegistration.Consumers.Match(
                some =>
                {
                    some.Assemblies.ToList().ForEach(a => opts.Discovery.IncludeAssembly(a));
                    WolverineConventions.RegisterListeners(opts, some.QueuePrefix, some.Assemblies);
                },
                () => { }
            );

            endpointsRegistration.SendConventions.Match(
                some =>
                    WolverineConventions.RegisterSendConventions(
                        opts,
                        some.QueuePrefix,
                        some.Assemblies
                    ),
                () => { }
            );

            endpointsRegistration.IntegrationEvents.Match(
                some =>
                    WolverineConventions.RegisterIntegrationEventPublishing(
                        opts,
                        some.QueuePrefix,
                        some.Assemblies
                    ),
                () => { }
            );

            opts.OnException<Ports.EventStore.ConcurrencyException>()
                .RetryWithCooldown([
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromMilliseconds(150),
                    TimeSpan.FromMilliseconds(500),
                ]);

            opts.OnException<Ports.MessageBus.PermanentProcessingFailureException>()
                .MoveToErrorQueue();
        });

        return services;
    }
}
