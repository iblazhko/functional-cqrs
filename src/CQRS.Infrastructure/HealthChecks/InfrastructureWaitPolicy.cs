namespace CQRS.Infrastructure;

using System;
using System.Linq;
using System.Net.Sockets;
using CQRS.Configuration;
using Polly;
using Polly.Contrib.WaitAndRetry;

public static class InfrastructureWaitPolicy
{
    public static void WaitForInfrastructureOrFail(CqrsSettings settings)
    {
        if (WaitForInfrastructure(settings))
        {
            Console.WriteLine(
                $"[{TimeProvider.System.GetUtcNow():O}] Infrastructure services ready"
            );
        }
        else
        {
            throw new InvalidOperationException("Infrastructure services not available");
        }
    }

    public static bool WaitForInfrastructure(CqrsSettings settings)
    {
        if (!settings.InfrastructureStartup.WaitOnStartup)
            return true;

        var policy = Policy
            .HandleResult(false)
            .WaitAndRetry(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), 5));

        return policy.Execute(() => IsInfrastructureAvailable(settings));
    }

    private static bool IsInfrastructureAvailable(CqrsSettings settings)
    {
        var infrastructureServicesAvailability = new[]
        {
            GetServiceAvailability(
                nameof(settings.MessageBus.RabbitMq),
                settings.MessageBus.RabbitMq.Endpoint.Host,
                settings.MessageBus.RabbitMq.Endpoint.Port
            ),
            GetServiceAvailability(
                nameof(settings.MartenDb),
                settings.MartenDb.Endpoint.Host,
                settings.MartenDb.Endpoint.Port
            ),
        };

        var unavailableMessages = infrastructureServicesAvailability
            .Where(x => !x.IsAvailable)
            .Select(x => $"Service {x.ServiceName} is not available at {x.Host}:{x.Port}")
            .ToArray();

        if (unavailableMessages.Length > 0)
        {
            var errorMessage = string.Join(";", unavailableMessages);
            // Proper logger is not available at this point
            Console.WriteLine($"[{TimeProvider.System.GetUtcNow():O}] {errorMessage}");
        }

        return unavailableMessages.Length == 0;
    }

    private static bool IsPortOpen(string host, int port, TimeSpan timeout)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(timeout);
            client.EndConnect(result);

            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[{TimeProvider.System.GetUtcNow():O}] Port check {host}:{port} failed — {ex.GetType().Name}: {ex.Message}"
            );
            return false;
        }
    }

    private static ServiceAvailabilityResult GetServiceAvailability(
        string serviceName,
        string host,
        int port
    ) => new(serviceName, host, port, IsPortOpen(host, port, PortCheckTimeout));

    private record ServiceAvailabilityResult(
        string ServiceName,
        string Host,
        int Port,
        bool IsAvailable
    );

    private static readonly TimeSpan PortCheckTimeout = TimeSpan.FromSeconds(3);
}
