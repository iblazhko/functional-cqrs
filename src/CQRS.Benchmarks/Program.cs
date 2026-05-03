using CQRS.Benchmarks;
using Spectre.Console;

var settings = BenchmarkSettingsResolver.GetSettings();
var client = new InventoryBenchmarkClient(settings);
var runner = new BenchmarkRunner(client, settings);

try
{
    AnsiConsole.Write(new Rule("[bold blue]CQRS System Benchmarks[/]").RuleStyle("blue"));
    AnsiConsole.MarkupLine(
        $"  API: [grey]{settings.ApiServiceUrl}[/]  |  App: [grey]{settings.AppServiceUrl}[/]"
    );
    AnsiConsole.MarkupLine(
        $"  Concurrency levels: [grey]{string.Join(", ", settings.ConcurrencyLevels)}[/]  |  Iterations per inventory: [grey]{settings.IterationsPerInventory}[/]"
    );
    AnsiConsole.WriteLine();

    var records = await runner.RunAsync();
    BenchmarkReport.Print(records, settings.ConcurrencyLevels);

    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Rule().RuleStyle("blue"));
    return 0;
}
catch (Exception ex)
{
    AnsiConsole.MarkupLine($"[red]Benchmark failed:[/] {Markup.Escape(ex.Message)}");
    return 1;
}
