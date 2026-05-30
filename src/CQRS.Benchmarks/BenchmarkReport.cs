using Spectre.Console;

namespace CQRS.Benchmarks;

public static class BenchmarkReport
{
    private static readonly string[] OutcomeOrder =
    [
        "Completed",
        "Rejected",
        "Failed",
        "Timeout",
        "Error",
    ];

    public static void Print(IReadOnlyList<BenchmarkRecord> records, int[] concurrencyLevels)
    {
        AnsiConsole.WriteLine();
        PrintSuccessRateTable(records, concurrencyLevels);
        AnsiConsole.WriteLine();
        PrintProcessingTimeTable(records, concurrencyLevels);
    }

    private static void PrintSuccessRateTable(
        IReadOnlyList<BenchmarkRecord> records,
        int[] concurrencyLevels
    )
    {
        var commandTypes = records.Select(r => r.CommandType).Distinct().OrderBy(x => x).ToList();

        var table = new Table();
        table.Title("Command Success Rate");
        table.AddColumn("Command Type");
        foreach (var level in concurrencyLevels)
            table.AddColumn($"Concurrency={level}");

        foreach (var cmd in commandTypes)
        {
            var row = new List<string> { cmd };
            foreach (var level in concurrencyLevels)
            {
                var subset = records
                    .Where(r => r.CommandType == cmd && r.ConcurrencyLevel == level)
                    .ToList();
                var total = subset.Count;
                var succeeded = subset.Count(r => r.Outcome == "Completed");
                row.Add(total > 0 ? $"{succeeded}/{total} ({100.0 * succeeded / total:F0}%)" : "-");
            }
            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);
    }

    private static void PrintProcessingTimeTable(
        IReadOnlyList<BenchmarkRecord> records,
        int[] concurrencyLevels
    )
    {
        var groups = records
            .GroupBy(r => (r.CommandType, r.Outcome))
            .OrderBy(g => g.Key.CommandType)
            .ThenBy(g =>
            {
                var idx = Array.IndexOf(OutcomeOrder, g.Key.Outcome);
                return idx < 0 ? OutcomeOrder.Length : idx;
            })
            .ToList();

        var table = new Table();
        table.Title("Processing Time (ms)");
        table.AddColumn("Command Type");
        table.AddColumn("Outcome");
        foreach (var level in concurrencyLevels)
            table.AddColumn($"Concurrency={level}");

        foreach (var group in groups)
        {
            var row = new List<string> { group.Key.CommandType, group.Key.Outcome };
            foreach (var level in concurrencyLevels)
            {
                var times = group
                    .Where(r => r.ConcurrencyLevel == level)
                    .Select(r => r.ElapsedMs)
                    .OrderBy(t => t)
                    .ToList();

                row.Add(times.Count == 0 ? "-" : FormatTimes(times));
            }
            table.AddRow(row.ToArray());
        }

        AnsiConsole.Write(table);
    }

    private static string FormatTimes(List<long> sortedTimes)
    {
        var avg = sortedTimes.Average();
        var p50 = sortedTimes[sortedTimes.Count / 2];
        var p95 = sortedTimes[(int)Math.Min(sortedTimes.Count * 0.95, sortedTimes.Count - 1)];
        return $"avg={avg:F0} p50={p50} p95={p95}";
    }
}
