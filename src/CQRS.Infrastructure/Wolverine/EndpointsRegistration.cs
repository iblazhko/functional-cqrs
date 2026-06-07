using System.Reflection;
using LanguageExt;

namespace CQRS.Infrastructure;

public sealed record EndpointsRegistration(Seq<Assembly> Assemblies, string QueuePrefix = "cqrs");

public sealed record HostEndpointsRegistration(
    Option<EndpointsRegistration> SendConventions,
    Option<EndpointsRegistration> Consumers,
    Option<EndpointsRegistration> IntegrationEvents = default
);
