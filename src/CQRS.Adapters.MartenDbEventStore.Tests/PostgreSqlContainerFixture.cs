using CQRS.Application.Inventory;
using CQRS.Domain.Inventory;
using CQRS.DTO;
using CQRS.Ports.EventStore;
using DotNet.Testcontainers.Configurations;
using JasperFx.Events;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace CQRS.Adapters.MartenDbEventStore.Tests;

[CollectionDefinition("PostgreSQL Integration")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture> { }

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    static PostgreSqlContainerFixture() => TestcontainersSettings.ResourceReaperEnabled = false;

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18.3")
            .Build();

    public IDocumentStore DocumentStore { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        DocumentStore = Marten.DocumentStore.For(opts =>
        {
            opts.Connection(_container.GetConnectionString());
            opts.UseSystemTextJsonForSerialization();
            opts.Events.StreamIdentity = StreamIdentity.AsString;
            opts.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
            opts.Events.AddEventTypes(
                typeof(IInventoryEventDto).Assembly
                    .GetTypes()
                    .Where(t =>
                        typeof(IInventoryEventDto).IsAssignableFrom(t)
                        && t is { IsClass: true, IsAbstract: false }
                    )
                    .ToArray()
            );
        });

        await DocumentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    }

    public MartenDbEventStoreAdapter<InventoryState, IInventoryEvent, IInventoryEventDto> CreateAdapter() =>
        new(DocumentStore, new NoOpEventPublisher<IInventoryEvent>(), TimeProvider.System, NullLoggerFactory.Instance);

    public async ValueTask DisposeAsync()
    {
        if (DocumentStore is not null)
        {
            await DocumentStore.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
