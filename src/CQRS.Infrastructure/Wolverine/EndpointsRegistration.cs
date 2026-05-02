namespace CQRS.Infrastructure;

using System.Reflection;
using LanguageExt;

public sealed record EndpointsRegistration(Seq<Assembly> Assemblies, string QueuePrefix = "cqrs");

public sealed record HostEndpointsRegistration(
    Option<EndpointsRegistration> SendConventions,
    Option<EndpointsRegistration> Consumers
);
