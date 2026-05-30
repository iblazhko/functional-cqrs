namespace CQRS.Application;

public interface ITimeProvider
{
    DateTimeOffset GetUtcNow();
    Domain.TimeZone TimeZone { get; }
}

public sealed class ApplicationTimeProvider : ITimeProvider
{
    private readonly Domain.TimeZone _timeZone;

    public ApplicationTimeProvider()
    {
        _timeZone = Domain
            .TimeZone.Create("Europe/London")
            .Match(
                Right: tz => tz,
                Left: _ =>
                    throw new InvalidOperationException(
                        "Invalid hardcoded timezone identifier: 'Europe/London'"
                    )
            );
    }

    public DateTimeOffset GetUtcNow() => TimeProvider.System.GetUtcNow();

    public Domain.TimeZone TimeZone => _timeZone;
}
