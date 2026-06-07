using System.Text.Json;
using System.Threading.Channels;
using CQRS.Domain.Inventory;
using CQRS.IntegrationEvents.Inventory.V1;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;

namespace CQRS.System.Integration.Tests;

[Collection(DockerComposeCollectionFixture.DockerComposeTestsCollection)]
public class IntegrationEventsTests(CqrsTestContainersFixture fixture) : IAsyncLifetime
{
    private const string ExchangeName = "cqrs.integration:inventory_updated";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private IConnection? _connection;
    private IChannel? _channel;
    private Channel<byte[]> _messages = Channel.CreateUnbounded<byte[]>();

    public async ValueTask InitializeAsync()
    {
        var rmq = fixture.RabbitMqSettings;
        var factory = new ConnectionFactory
        {
            HostName = rmq.Endpoint.Host,
            Port = rmq.Endpoint.Port,
            UserName = rmq.Username,
            Password = rmq.Password,
            VirtualHost = rmq.VirtualHost,
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();
        _messages = Channel.CreateUnbounded<byte[]>();

        var queueResult = await _channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null,
            passive: false,
            noWait: false
        );

        await _channel.QueueBindAsync(
            queue: queueResult.QueueName,
            exchange: ExchangeName,
            routingKey: string.Empty,
            arguments: null,
            noWait: false
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            await _messages.Writer.WriteAsync(args.Body.ToArray());
        };

        await _channel.BasicConsumeAsync(
            queue: queueResult.QueueName,
            autoAck: true,
            consumerTag: string.Empty,
            noLocal: false,
            exclusive: false,
            arguments: null,
            consumer: consumer
        );
    }

    public async ValueTask DisposeAsync()
    {
        _messages.Writer.TryComplete();
        if (_channel is not null)
        {
            await _channel.CloseAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
        }
    }

    private async Task<InventoryUpdated> ReceiveIntegrationEvent(
        CancellationToken cancellationToken = default
    )
    {
        var bytes = await _messages.Reader.ReadAsync(cancellationToken);
        return JsonSerializer.Deserialize<InventoryUpdated>(bytes, JsonOptions)!;
    }

    [Fact]
    public async Task CreateInventory_ShouldPublishInventoryUpdated()
    {
        const string inventoryName = "INT-EVENT-CREATE";
        var inventoryId = InventoryId.NewId().ToString();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var response = await fixture.SUT.CreateInventory(inventoryName, inventoryId);
        await fixture.SUT.WaitForCommandProcessed(response.CommandId!.Value);

        var evt = await ReceiveIntegrationEvent(cts.Token);

        evt.InventoryId.ShouldBe(inventoryId);
        evt.Name.ShouldBe(inventoryName);
        evt.StockQuantity.ShouldBe(0);
        evt.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task RenameInventory_ShouldPublishInventoryUpdatedWithNewName()
    {
        const string initialName = "INT-EVENT-RENAME-BEFORE";
        const string updatedName = "INT-EVENT-RENAMED";
        var inventoryId = InventoryId.NewId().ToString();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var createResponse = await fixture.SUT.CreateInventory(initialName, inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await ReceiveIntegrationEvent(cts.Token); // drain create event

        var renameResponse = await fixture.SUT.RenameInventory(inventoryId, updatedName);
        await fixture.SUT.WaitForCommandProcessed(renameResponse.CommandId!.Value);

        var evt = await ReceiveIntegrationEvent(cts.Token);

        evt.InventoryId.ShouldBe(inventoryId);
        evt.Name.ShouldBe(updatedName);
        evt.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task AddItemsToInventory_ShouldPublishInventoryUpdatedWithUpdatedQuantity()
    {
        const int itemCount = 42;
        var inventoryId = InventoryId.NewId().ToString();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var createResponse = await fixture.SUT.CreateInventory("INT-EVENT-ADD-ITEMS", inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await ReceiveIntegrationEvent(cts.Token); // drain create event

        var addResponse = await fixture.SUT.AddItemsToInventory(inventoryId, itemCount);
        await fixture.SUT.WaitForCommandProcessed(addResponse.CommandId!.Value);

        var evt = await ReceiveIntegrationEvent(cts.Token);

        evt.InventoryId.ShouldBe(inventoryId);
        evt.StockQuantity.ShouldBe(itemCount);
        evt.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveItemsFromInventory_ShouldPublishInventoryUpdatedWithUpdatedQuantity()
    {
        const int addCount = 20;
        const int removeCount = 7;
        var inventoryId = InventoryId.NewId().ToString();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var createResponse = await fixture.SUT.CreateInventory(
            "INT-EVENT-REMOVE-ITEMS",
            inventoryId
        );
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await ReceiveIntegrationEvent(cts.Token); // drain create event

        var addResponse = await fixture.SUT.AddItemsToInventory(inventoryId, addCount);
        await fixture.SUT.WaitForCommandProcessed(addResponse.CommandId!.Value);
        await ReceiveIntegrationEvent(cts.Token); // drain add event

        var removeResponse = await fixture.SUT.RemoveItemsFromInventory(inventoryId, removeCount);
        await fixture.SUT.WaitForCommandProcessed(removeResponse.CommandId!.Value);

        var evt = await ReceiveIntegrationEvent(cts.Token);

        evt.InventoryId.ShouldBe(inventoryId);
        evt.StockQuantity.ShouldBe(addCount - removeCount);
        evt.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task DeactivateInventory_ShouldPublishInventoryUpdatedWithIsActiveFalse()
    {
        var inventoryId = InventoryId.NewId().ToString();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var createResponse = await fixture.SUT.CreateInventory("INT-EVENT-DEACTIVATE", inventoryId);
        await fixture.SUT.WaitForCommandProcessed(createResponse.CommandId!.Value);
        await ReceiveIntegrationEvent(cts.Token); // drain create event

        var deactivateResponse = await fixture.SUT.DeactivateInventory(inventoryId);
        await fixture.SUT.WaitForCommandProcessed(deactivateResponse.CommandId!.Value);

        var evt = await ReceiveIntegrationEvent(cts.Token);

        evt.InventoryId.ShouldBe(inventoryId);
        evt.IsActive.ShouldBeFalse();
    }
}
