using Microsoft.Extensions.Configuration;

namespace CQRS.Configuration;

public static class SettingsResolver
{
    private const string SettingsSectionName = nameof(CQRS);

    public static CqrsSettings GetSettings(string sectionName = SettingsSectionName)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables($"{SettingsSectionName}_")
            .Build();

        return config.GetSection(sectionName).Get<CqrsSettings>()
            ?? throw new InvalidOperationException(
                $"Could not read configuration settings section {sectionName}"
            );
    }
}
