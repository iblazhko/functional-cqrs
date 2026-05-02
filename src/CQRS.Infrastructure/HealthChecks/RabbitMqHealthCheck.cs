namespace CQRS.Infrastructure;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class RabbitMqHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => Task.FromResult(HealthCheckResult.Healthy($"{nameof(RabbitMqHealthCheck)}: ????"));
}
