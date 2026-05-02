using System.Text;

namespace CQRS.Configuration;

public sealed class CqrsSettings
{
    public string ServiceUrl { get; init; } = string.Empty;
    public MartenDbSettings MartenDb { get; init; } = new();
    public MessageBusSettings MessageBus { get; init; } = new();
    public InfrastructureStartupSettings InfrastructureStartup { get; init; } = new();
    public LoggingSettings Logging { get; init; } = new();

    public override string ToString() =>
        new StringBuilder()
            .AppendSettingsTitle(nameof(CQRS))
            .AppendSettingValue(() => ServiceUrl)
            .AppendLine()
            .AppendSettingsSection(() => MartenDb)
            .AppendSettingsSection(() => MessageBus)
            .AppendSettingsSection(() => InfrastructureStartup)
            .AppendSettingsSection(() => Logging)
            .ToString();
}
