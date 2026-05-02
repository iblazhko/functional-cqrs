namespace CQRS.Infrastructure;

using CQRS.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

public static class HealthCheckConfigurator
{
    public static IServiceCollection AddApplicationHealthChecks(
        this IServiceCollection services,
        CqrsSettings settings
    )
    {
        services.AddSingleton<IConnection>(_ =>
            new ConnectionFactory { Uri = new Uri(settings.MessageBus.RabbitMq.GetAmqpUrl()) }
                .CreateConnectionAsync()
                .GetAwaiter()
                .GetResult()
        );

        services.AddHealthChecks().AddNpgSql(settings.MartenDb.GetConnectionString()).AddRabbitMQ();

        return services;
    }
}
