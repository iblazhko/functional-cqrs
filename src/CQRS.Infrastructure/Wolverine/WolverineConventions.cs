namespace CQRS.Infrastructure;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using CQRS.DTO;
using Wolverine;
using Wolverine.RabbitMQ;

internal static class WolverineConventions
{
    private static readonly Type CommandDtoType = typeof(ICqrsCommandDto);
    private static readonly Type EnvelopeType = typeof(Envelope);
    private static readonly string[] HandlerMethodNames =
    [
        "Handle",
        "HandleAsync",
        "Consume",
        "ConsumeAsync",
    ];

    internal static string ToQueueName(string queuePrefix, string typeName)
    {
        var snakeCase = ToSnakeCase(typeName);
        var trimmed = RemoveSuffix(snakeCase, ["_command", "_event"]);
        return $"{queuePrefix}:{trimmed}";
    }

    internal static void RegisterListeners(
        WolverineOptions opts,
        string queuePrefix,
        IEnumerable<Assembly> assemblies
    )
    {
        foreach (var asm in assemblies)
        foreach (var messageType in MessageTypesFromHandlerAssembly(asm))
        {
            var queueName = ToQueueName(queuePrefix, messageType.Name);
            opts.ListenToRabbitQueue(queueName);
        }
    }

    internal static void RegisterSendConventions(
        WolverineOptions opts,
        string queuePrefix,
        IEnumerable<Assembly> assemblies
    )
    {
        foreach (var asm in assemblies)
        foreach (var commandType in CommandTypesFromAssembly(asm))
        {
            var queueName = ToQueueName(queuePrefix, commandType.Name);
            opts.PublishMessage(commandType).ToRabbitQueue(queueName);
        }
    }

    private static IEnumerable<Type> MessageTypesFromHandlerAssembly(Assembly asm) =>
        asm.GetTypes()
            .Where(t => !t.IsAbstract && t.IsPublic)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(m => HandlerMethodNames.Contains(m.Name))
            .SelectMany(m => m.GetParameters())
            .Where(p =>
                !EnvelopeType.IsAssignableFrom(p.ParameterType)
                && !p.ParameterType.IsValueType
                && p.ParameterType != typeof(CancellationToken)
            )
            .Select(p => p.ParameterType)
            .Distinct();

    internal static void RegisterIntegrationEventPublishing(
        WolverineOptions opts,
        string queuePrefix,
        IEnumerable<Assembly> assemblies
    )
    {
        var exchangePrefix = $"{queuePrefix}.integration";
        foreach (var asm in assemblies)
        foreach (var eventType in IntegrationEventTypesFromAssembly(asm))
        {
            var exchangeName = $"{exchangePrefix}:{ToSnakeCase(eventType.Name)}";
            opts.PublishMessage(eventType).ToRabbitExchange(exchangeName);
        }
    }

    private static IEnumerable<Type> IntegrationEventTypesFromAssembly(Assembly asm) =>
        asm.GetTypes()
            .Where(t =>
                !t.IsAbstract && t.IsPublic && !t.IsInterface && !t.IsGenericType && !t.IsNested
            );

    private static IEnumerable<Type> CommandTypesFromAssembly(Assembly asm) =>
        asm.GetTypes().Where(t => !t.IsAbstract && CommandDtoType.IsAssignableFrom(t));

    private static string ToSnakeCase(string typeName)
    {
        var sb = new StringBuilder();
        foreach (var c in typeName)
        {
            if (char.IsUpper(c) && sb.Length > 0)
                sb.Append('_');
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }

    private static string RemoveSuffix(string s, string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            if (s.EndsWith(suffix, StringComparison.Ordinal))
                return s[..^suffix.Length];
        }
        return s;
    }
}
