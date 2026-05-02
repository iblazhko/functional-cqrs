using DotNet.Testcontainers.Configurations;
using Marten;
using Testcontainers.PostgreSql;

namespace CQRS.Adapters.MartenDbProjectionStore.Tests;

[CollectionDefinition("PostgreSQL Integration")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture> { }

public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    static PostgreSqlContainerFixture() => TestcontainersSettings.ResourceReaperEnabled = false;

    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18.3")
            .Build();

    public IDocumentStore DocumentStore { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        DocumentStore = Marten.DocumentStore.For(opts =>
        {
            opts.Connection(_container.GetConnectionString());
            opts.UseSystemTextJsonForSerialization();
            opts.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
        });

        await DocumentStore.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
    }

    public async Task DisposeAsync()
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
