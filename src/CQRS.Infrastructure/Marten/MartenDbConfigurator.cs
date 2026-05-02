namespace CQRS.Infrastructure;

using CQRS.Configuration;
using JasperFx.Events;
using Marten;
using Microsoft.Extensions.DependencyInjection;

public static class MartenDbConfigurator
{
    public static IServiceCollection AddApplicationMartenDb(
        this IServiceCollection services,
        MartenDbSettings settings
    )
    {
        services
            .AddMarten(options =>
            {
                options.Connection(settings.GetConnectionString());
                options.UseSystemTextJsonForSerialization();

                options.Events.StreamIdentity = StreamIdentity.AsString;
                options.Events.MetadataConfig.HeadersEnabled = true;
                options.Events.MetadataConfig.CausationIdEnabled = true;
                options.Events.MetadataConfig.CorrelationIdEnabled = true;

                // AutoCreateSchemaObjects most likely should be turned off in a real deployment
                options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
            })
            .ApplyAllDatabaseChangesOnStartup();

        return services;
    }
}
