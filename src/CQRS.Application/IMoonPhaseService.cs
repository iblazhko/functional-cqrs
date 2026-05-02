using CQRS.Domain;

namespace CQRS.Application;

public interface IMoonPhaseService
{
    Task<MoonPhase> GetMoonPhase(Domain.TimeZone timeZone, DateTimeOffset time);
}

public class MoonPhaseService(Random random) : IMoonPhaseService
{
    public Task<MoonPhase> GetMoonPhase(Domain.TimeZone timeZone, DateTimeOffset time) =>
        Task.FromResult(MoonPhaseValues[random.Next(0, MoonPhaseValues.Length)]);

    private static readonly MoonPhase[] MoonPhaseValues =
        [
            MoonPhase.NewMoon,
            MoonPhase.FirstQuarter,
            MoonPhase.FullMoon,
            MoonPhase.LastQuarter
        ];
}
