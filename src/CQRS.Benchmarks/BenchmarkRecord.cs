namespace CQRS.Benchmarks;

public sealed record BenchmarkRecord(
    int ConcurrencyLevel,
    string CommandType,
    string Outcome,
    long ElapsedMs
);
