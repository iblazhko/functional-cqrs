using CQRS.API;
using CQRS.Configuration;
using CQRS.DTO;
using CQRS.Infrastructure;
using CQRS.Mapping;
using static LanguageExt.Prelude;

var settings = SettingsResolver.GetSettings("CqrsApi");
Console.WriteLine(settings.ToString()); // Proper logger in not available at this point
InfrastructureWaitPolicy.WaitForInfrastructureOrFail(settings);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(settings.ServiceUrl);

// csharpier-ignore
builder.Services
    .AddApiServices()
    .AddMappingServices()
    .AddCqrsMessageBus(
         new HostEndpointsRegistration(
             new EndpointsRegistration([typeof(IInventoryCommandDto).Assembly]),
             None),
         settings.MessageBus)
    .AddCqrsProjectionReader(settings.MartenDb)
    .AddApplicationSerilog(settings.Logging)
    .AddApplicationHealthChecks(settings)
    .AddApplicationOpenTelemetry("cqrs-api");

var app = builder.Build();
app.AddApiEndpoints();
app.UseHealthChecks("/health");

app.Run();
