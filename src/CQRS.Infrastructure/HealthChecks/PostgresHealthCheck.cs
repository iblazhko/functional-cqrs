namespace CQRS.Infrastructure;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public class PostgresHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => Task.FromResult(HealthCheckResult.Healthy($"{nameof(PostgresHealthCheck)}: ????"));
}
