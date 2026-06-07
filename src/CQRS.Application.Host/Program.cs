using System.Reflection;
using CQRS.Application;
using CQRS.Application.CommandProcessingStatusRecording;
using CQRS.Application.WolverineHandlers;
using CQRS.Configuration;
using CQRS.DTO;
using CQRS.Infrastructure;
using CQRS.IntegrationEvents.Inventory.V1;
using CQRS.Mapping;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

var settings = SettingsResolver.GetSettings("CqrsApp");
Console.WriteLine(settings.ToString()); // Proper logger in not available at this point
InfrastructureWaitPolicy.WaitForInfrastructureOrFail(settings);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(settings.ServiceUrl);

var commandsAssembly = typeof(IInventoryCommandDto).Assembly;
var commandConsumersAssembly = typeof(InventoryCommandConsumer).Assembly;
var integrationEventsAssembly = typeof(InventoryUpdated).Assembly;

// csharpier-ignore
builder.Services
    .AddApplicationServices()
    .AddMappingServices()
    .AddCqrsMessageBus(
         new HostEndpointsRegistration(
             new EndpointsRegistration(new Seq<Assembly>([commandsAssembly])),
             new EndpointsRegistration(new Seq<Assembly>([commandConsumersAssembly])),
             new EndpointsRegistration(new Seq<Assembly>([integrationEventsAssembly]))),
         settings.MessageBus)
    .AddCqrsEventStore(settings.MartenDb)
    .AddMartenDbCommandProcessingStatus(settings.MartenDb)
    .AddApplicationSerilog(settings.Logging)
    .AddApplicationHealthChecks(settings)
    .AddApplicationOpenTelemetry("cqrs-application");

var app = builder.Build();

app.UseHealthChecks("/health");

app.MapGet(
    "/commands/{commandId:guid}/status",
    async (Guid commandId, [FromServices] ICommandProcessingStatusQueryService queryService) =>
        (await queryService.GetCommandProcessingStatus(commandId)).Match<IResult>(
            vm => Results.Ok(vm),
            () => Results.NotFound()
        )
);

app.Run();
