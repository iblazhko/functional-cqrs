namespace CQRS.Infrastructure;

using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Application.Inventory;
using CQRS.Configuration;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.Projections.Inventory.V1;
using CQRS.Projections.ViewModels.Inventory.V1;
using CQRS.Adapters.MartenDbEventStore;
using JasperFx.Events;
using JasperFx.Events.Projections;
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

                options
                    .Schema.For<CommandProcessingStatusViewModel>()
                    .Identity(x => x.CommandId)
                    .Index(x => x.CorrelationId);

                options.Schema.For<InventoryViewModel>().Identity(x => x.Id);
                options.Projections.Add(
                    new MartenDbProjectionAdapter<InventoryViewModel, IInventoryEvent, IInventoryEventDto>(
                        new InventoryViewModelProjection(),
                        new EventStoreInventoryEventMapper(),
                        InventoryEventStreamId.GetDocumentId
                    ),
                    ProjectionLifecycle.Inline,
                    "InventoryProjection"
                );

                // AutoCreateSchemaObjects most likely should be turned off in a real deployment
                options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
            })
            .ApplyAllDatabaseChangesOnStartup();

        return services;
    }
}
