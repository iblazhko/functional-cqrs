using System.Text;

namespace CQRS.Configuration;

public sealed record EndpointSettings
{
    public string Scheme { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 0;

    public override string ToString() => $"{Scheme}://{Host}:{Port}";
}

public sealed record MartenDbSettings
{
    public EndpointSettings Endpoint { get; init; } = new();
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Database { get; init; } = string.Empty;
    public string ConnectionOptions { get; init; } = string.Empty;

    public string GetConnectionString() =>
        $"Host={Endpoint.Host};Port={Endpoint.Port};Database={Database};Username={Username};Password={Password};{ConnectionOptions}";

    public override string ToString() =>
        new StringBuilder()
            .AppendSettingValue(() => Endpoint)
            .AppendSettingValue(() => Database)
            .ToString();
}

public sealed record RabbitMqSettings
{
    public EndpointSettings Endpoint { get; init; } = new();
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string VirtualHost { get; init; } = string.Empty;

    public string GetAmqpUrl() =>
        $"amqp://{Uri.EscapeDataString(Username)}:{Uri.EscapeDataString(Password)}@{Endpoint.Host}:{Endpoint.Port}/{VirtualHost}";

    public string GetRabbitMqUrl() => $"rabbitmq://{Endpoint.Host}:{Endpoint.Port}/{VirtualHost}";

    public override string ToString() =>
        new StringBuilder()
            .AppendSettingValue(() => Endpoint)
            .AppendSettingValue(() => VirtualHost)
            .ToString();
}

public sealed record MessageBusSettings
{
    public RabbitMqSettings RabbitMq { get; init; } = new();

    public override string ToString() => RabbitMq.ToString();
}

public sealed record InfrastructureStartupSettings
{
    public bool WaitOnStartup { get; init; } = false;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.Zero;
    public int RetryCount { get; init; } = 0;

    public override string ToString() =>
        new StringBuilder()
            .AppendSettingValue(() => WaitOnStartup)
            .AppendSettingValue(() => RetryDelay)
            .AppendSettingValue(() => RetryCount)
            .ToString();
}

public sealed record LoggingSettings
{
    public string Level { get; init; } = string.Empty;

    public override string ToString() =>
        new StringBuilder().AppendSettingValue(() => Level).ToString();
}
