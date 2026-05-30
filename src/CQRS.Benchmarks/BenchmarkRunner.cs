using Flurl.Http;
using Spectre.Console;

namespace CQRS.Benchmarks;

public sealed class BenchmarkRunner(InventoryBenchmarkClient client, BenchmarkSettings settings)
{
    public async Task<List<BenchmarkRecord>> RunAsync()
    {
        await CheckHealthOrThrowAsync();

        var allRecords = new List<BenchmarkRecord>();
        foreach (var concurrencyLevel in settings.ConcurrencyLevels)
        {
            AnsiConsole.MarkupLine(
                $"Running [bold]{concurrencyLevel}[/] concurrent scenario(s)..."
            );
            var tasks = Enumerable
                .Range(0, concurrencyLevel)
                .Select(_ => new InventoryScenario(client, settings).RunAsync(concurrencyLevel));
            var results = await Task.WhenAll(tasks);
            allRecords.AddRange(results.SelectMany(r => r));
        }

        return allRecords;
    }

    private async Task CheckHealthOrThrowAsync()
    {
        AnsiConsole.MarkupLine("Checking system health...");

        var apiHealthy = await IsHealthyAsync(settings.ApiServiceUrl);
        if (!apiHealthy)
            throw new InvalidOperationException(
                $"API host not healthy at {settings.ApiServiceUrl}/health"
            );

        var appHealthy = await IsHealthyAsync(settings.AppServiceUrl);
        if (!appHealthy)
            throw new InvalidOperationException(
                $"Application host not healthy at {settings.AppServiceUrl}/health"
            );

        AnsiConsole.MarkupLine("[green]System is healthy.[/]");
    }

    private static async Task<bool> IsHealthyAsync(string serviceUrl)
    {
        try
        {
            var response = await $"{serviceUrl}/health".GetStringAsync();
            return response.Contains("Healthy", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
