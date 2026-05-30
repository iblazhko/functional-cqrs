namespace CQRS.Domain;

public abstract record MoonPhase
{
    internal sealed record NewMoonPhase : MoonPhase;

    public static readonly MoonPhase NewMoon = new NewMoonPhase();

    internal sealed record FirstQuarterPhase : MoonPhase;

    public static readonly MoonPhase FirstQuarter = new FirstQuarterPhase();

    internal sealed record FullMoonPhase : MoonPhase;

    public static readonly MoonPhase FullMoon = new FullMoonPhase();

    internal sealed record LastQuarterPhase : MoonPhase;

    public static readonly MoonPhase LastQuarter = new LastQuarterPhase();
}

public static class MoonPhaseExtensions
{
    public static bool IsNewMoon(this MoonPhase phase) => phase is MoonPhase.NewMoonPhase;

    public static bool IsFirstQuarter(this MoonPhase phase) => phase is MoonPhase.FirstQuarterPhase;

    public static bool IsFullMoon(this MoonPhase phase) => phase is MoonPhase.FullMoonPhase;

    public static bool IsLastQuarter(this MoonPhase phase) => phase is MoonPhase.LastQuarterPhase;
}
