namespace CQRS.Application;

public interface ITimeProvider
{
    DateTimeOffset GetUtcNow();
    Domain.TimeZone TimeZone { get; }
}

public sealed class ApplicationTimeProvider : ITimeProvider
{
    public DateTimeOffset GetUtcNow() => TimeProvider.System.GetUtcNow();

    public Domain.TimeZone TimeZone => LondonTimeZone;

    private static readonly Domain.TimeZone LondonTimeZone =
        Domain.TimeZone.Create("Europe/London")
            .Match(
                Right: tz => tz,
                Left: _ => throw new InvalidOperationException("Invalid hardcoded timezone identifier")
            );
}
