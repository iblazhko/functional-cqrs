using CQRS.Configuration;

namespace CQRS.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Enrichers.OpenTelemetry;
using Serilog.Events;

public static class SerilogConfigurator
{
    public static IServiceCollection AddApplicationSerilog(this IServiceCollection services, LoggingSettings settings)
    {
        var configuration = new LoggerConfiguration();
        configuration = SetMinimumLevel(configuration, settings.Level);

        Log.Logger = configuration
            .Enrich.FromLogContext()
            .Enrich.WithOpenTelemetryTraceId()
            .Enrich.WithOpenTelemetrySpanId()
            .WriteTo.Console()
            .CreateLogger();

        services.AddSerilog();

        return services;
    }

    static LoggerConfiguration SetMinimumLevel(LoggerConfiguration configuration, string level) =>
        level?.ToUpperInvariant() switch
        {
            "VERBOSE" =>
                configuration
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Verbose),

            "DEBUG" =>
                configuration
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug),

            "INFO" or "INFORMATION" =>
                configuration
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information),

            "WARN" or "WARNING" =>
                configuration
                    .MinimumLevel.Warning()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning),

            "ERR" or "ERROR" =>
                configuration
                    .MinimumLevel.Error()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Error),

            "FATAL" =>
                configuration
                    .MinimumLevel.Fatal()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Fatal),

            _ =>
                configuration
                    .MinimumLevel.Warning()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        };
}
