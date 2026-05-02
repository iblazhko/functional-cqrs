using Microsoft.Extensions.Configuration;

namespace CQRS.CLI;

public sealed record CliSettings
{
    public string ApiServiceUrl { get; init; } = "http://localhost:17322";
    public string AppServiceUrl { get; init; } = "http://localhost:17321";
}

public static class CliSettingsResolver
{
    public static CliSettings GetSettings()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables("CQRS_")
            .Build();

        return config.GetSection("CqrsCli").Get<CliSettings>()
            ?? throw new InvalidOperationException("Could not read CqrsCli configuration");
    }
}
