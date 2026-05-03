using Microsoft.Extensions.Configuration;

namespace CQRS.Benchmarks;

public sealed record BenchmarkSettings
{
    public string ApiServiceUrl { get; init; } = "http://localhost:17322";
    public string AppServiceUrl { get; init; } = "http://localhost:17321";
    public int[] ConcurrencyLevels { get; init; } = [1, 5, 10];
    public int IterationsPerInventory { get; init; } = 10;
    public int RenameEveryNIterations { get; init; } = 3;
    public int CommandTimeoutSeconds { get; init; } = 30;
    public int ItemsToAddPerIteration { get; init; } = 10;
    public int ItemsToRemovePerIteration { get; init; } = 3;
}

public static class BenchmarkSettingsResolver
{
    public static BenchmarkSettings GetSettings()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables("CQRS_")
            .Build();

        return config.GetSection("CqrsBenchmarks").Get<BenchmarkSettings>()
            ?? new BenchmarkSettings();
    }
}
