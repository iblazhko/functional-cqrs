namespace CQRS.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

public static class OpenTelemetryConfigurator
{
    public static IServiceCollection AddApplicationOpenTelemetry(
        this IServiceCollection services,
        string serviceName
    )
    {
        services
            .AddOpenTelemetry()
            .WithTracing(tracing =>
                tracing
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource("Wolverine")
                    .AddSource("Marten")
                    .AddSource("Npgsql")
                    .AddOtlpExporter()
            );

        return services;
    }
}
