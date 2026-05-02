using System.Reflection;
using CQRS.API;
using CQRS.Application;
using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Application.WolverineHandlers;
using CQRS.Configuration;
using CQRS.Domain;
using CQRS.DTO;
using CQRS.Infrastructure;
using CQRS.Mapping;
using CQRS.Projections.WolverineHandlers;
using DotNet.Testcontainers.Configurations;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using static LanguageExt.Prelude;

namespace CQRS.System.Integration.Tests;

// ReSharper disable once ClassNeverInstantiated.Global
public class CqrsTestContainersFixture : IAsyncLifetime
{
    static CqrsTestContainersFixture() => TestcontainersSettings.ResourceReaperEnabled = false;

    private readonly MartenDbSettings MartenDbSettingsTemplate =
        new()
        {
            Endpoint = new EndpointSettings
            {
                Host = "localhost",
                Port = 5432
            },
            Username = "pgadmin",
            Password = "changeit",
            Database = "cqrs"
        };

    private readonly RabbitMqSettings RabbitMqSettingsTemplate =
        new()
        {
            Endpoint = new EndpointSettings
            {
                Host = "localhost",
                Port = 5672
            },
            Username = "rmqadmin",
            Password = "changeit",
            VirtualHost = "cqrs"
        };

    private PostgreSqlContainer? _postgres;
    private RabbitMqContainer? _rabbitmq;
    private WebApplication? _appHost;
    private WebApplication? _apiHost;

    // ReSharper disable once InconsistentNaming
    public CqrsSystem SUT { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _postgres = BuildPostgresContainer(MartenDbSettingsTemplate);
        _rabbitmq = BuildRabbitMqContainer(RabbitMqSettingsTemplate);

        await _postgres.StartAsync();
        await _rabbitmq.StartAsync();

        var martenDbSettings = BuildMartenDbSettings(MartenDbSettingsTemplate, _postgres);
        var messageBusSettings = BuildMessageBusSettings(RabbitMqSettingsTemplate, _rabbitmq);

        _appHost = BuildApplicationHost(martenDbSettings, messageBusSettings);
        _apiHost = BuildApiHost(martenDbSettings, messageBusSettings);

        await _appHost.StartAsync();
        await _apiHost.StartAsync();

        SUT = new CqrsSystem(
            new Uri(GetBoundAddress(_apiHost)),
            new Uri(GetBoundAddress(_appHost))
        );

        var ready = await SUT.WaitUntilReady();
        if (!ready)
            throw new InvalidOperationException("CQRS system did not become ready in time.");
    }

    public async Task DisposeAsync()
    {
        if (_apiHost is not null)
        {
             await _apiHost.StopAsync();
             await _apiHost.DisposeAsync();
        }

        if (_appHost is not null)
        {
            await _appHost.StopAsync();
            await _appHost.DisposeAsync();
        }

        if (_rabbitmq is not null)
        {
            await _rabbitmq.StopAsync();
            await _rabbitmq.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.StopAsync();
            await _postgres.DisposeAsync();
        }
    }

    MartenDbSettings BuildMartenDbSettings(MartenDbSettings settings, PostgreSqlContainer container) =>
        settings with
        {
            Endpoint = new EndpointSettings
            {
                Host = container.Hostname,
                Port = container.GetMappedPublicPort(5432)
            }
        };

    MessageBusSettings BuildMessageBusSettings(RabbitMqSettings settings, RabbitMqContainer container) =>
        new()
        {
            RabbitMq = BuildRabbitMqSettings(settings, container)
        };

    RabbitMqSettings BuildRabbitMqSettings(RabbitMqSettings settings, RabbitMqContainer container) =>
        settings with
        {
            Endpoint = new EndpointSettings
            {
                Host = container.Hostname,
                Port = container.GetMappedPublicPort(5672)
            }
        };

    PostgreSqlContainer BuildPostgresContainer(MartenDbSettings martenDbSettings) =>
        new PostgreSqlBuilder("postgres:18.3")
            .WithUsername(martenDbSettings.Username)
            .WithPassword(martenDbSettings.Password)
            .WithDatabase(martenDbSettings.Database)
            .Build();

    RabbitMqContainer BuildRabbitMqContainer(RabbitMqSettings rabbitMqSettings) =>
        new RabbitMqBuilder("rabbitmq:4.2-management")
            .WithUsername(rabbitMqSettings.Username)
            .WithPassword(rabbitMqSettings.Password)
            .WithEnvironment("RABBITMQ_DEFAULT_VHOST", rabbitMqSettings.VirtualHost)
            .Build();

    private static WebApplication BuildApplicationHost(MartenDbSettings martenDb, MessageBusSettings messageBus)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        var commandsAssembly = typeof(IInventoryCommandDto).Assembly;
        var commandConsumersAssembly = typeof(InventoryCommandConsumer).Assembly;
        var projectionConsumersAssembly = typeof(InventoryEventConsumer).Assembly;

        // csharpier-ignore
        builder.Services
            .AddApplicationServices()
            .AddSingleton<IMoonPhaseService>(new MoonPhaseServiceStub(MoonPhase.NewMoon))
            .AddMappingServices()
            .AddCqrsMessageBus(
                new HostEndpointsRegistration(
                    new EndpointsRegistration(new Seq<Assembly>([commandsAssembly])),
                    new EndpointsRegistration(new Seq<Assembly>([commandConsumersAssembly, projectionConsumersAssembly]))),
                messageBus)
            .AddCqrsEventStore(martenDb)
            .AddCqrsProjectionStore(martenDb)
            .AddApplicationSerilog(new LoggingSettings { Level = "ERROR" })
            .AddApplicationHealthChecks(BuildSettings(martenDb, messageBus));

        var app = builder.Build();
        app.UseHealthChecks("/health");
        app.MapGet(
            "/commands/{commandId:guid}/status",
            async (Guid commandId, ICommandProcessingStatusQueryService queryService) =>
                (await queryService.GetCommandProcessingStatus(commandId))
                    .Match<IResult>(vm => Results.Ok(vm), () => Results.NotFound())
        );
        return app;
    }

    private static WebApplication BuildApiHost(MartenDbSettings martenDb, MessageBusSettings messageBus)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // csharpier-ignore
        builder.Services
            .AddApiServices()
            .AddMappingServices()
            .AddCqrsMessageBus(
                new HostEndpointsRegistration(
                    new EndpointsRegistration(new Seq<Assembly>([typeof(IInventoryCommandDto).Assembly])),
                    None),
                messageBus)
            .AddCqrsProjectionStore(martenDb)
            .AddApplicationHealthChecks(BuildSettings(martenDb, messageBus));

        var app = builder.Build();
        app.AddApiEndpoints();
        app.UseHealthChecks("/health");
        return app;
    }

    private static CqrsSettings BuildSettings(MartenDbSettings martenDb, MessageBusSettings messageBus) =>
        new() { MartenDb = martenDb, MessageBus = messageBus };

    private static string GetBoundAddress(WebApplication app) =>
        app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses
            .First();
}

file sealed class MoonPhaseServiceStub(MoonPhase phase) : IMoonPhaseService
{
    public Task<MoonPhase> GetMoonPhase(CQRS.Domain.TimeZone timeZone, DateTimeOffset time) =>
        Task.FromResult(phase);
}

[CollectionDefinition(DockerComposeCollectionFixture.DockerComposeTestsCollection)]
public class DockerComposeCollectionFixture : ICollectionFixture<CqrsTestContainersFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.

    public const string DockerComposeTestsCollection = "DockerCompose Tests";
}
