using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace CQRS.System.Architecture.Tests;

public sealed class DependenciesTest
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies([
            .. new[]
            {
                "CQRS.Adapters.InMemoryEventStore",
                "CQRS.Adapters.MartenDbEventStore",
                "CQRS.Adapters.WolverineMessageBus",
                "CQRS.API",
                "CQRS.API.Host",
                "CQRS.Application",
                "CQRS.Application.CommandProcessingStatusRecording",
                "CQRS.Application.Host",
                "CQRS.Application.WolverineHandlers",
                "CQRS.CLI",
                "CQRS.Configuration",
                "CQRS.Domain",
                "CQRS.DTO",
                "CQRS.EntityIds",
                "CQRS.IntegrationEvents",
                "CQRS.Infrastructure",
                "CQRS.Mapping",
                "CQRS.Ports.EventStore",
                "CQRS.Ports.MessageBus",
                "CQRS.Projections",
                "CQRS.Projections.Repositories",
                "CQRS.Projections.ViewModels",
            }.Select(global::System.Reflection.Assembly.Load),
        ])
        .Build();

    private readonly IObjectProvider<IType> DomainLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Domain")
        .Or()
        .ResideInAssembly("CQRS.EntityIds")
        .Or()
        .ResideInAssembly("CQRS.Mapping")
        .As("Domain Layer");

    private readonly IObjectProvider<IType> DtoLayer = Types()
        .That()
        .ResideInAssembly("CQRS.DTO")
        .Or()
        .ResideInAssembly("CQRS.IntegrationEvents")
        .As("DTO Layer");

    private readonly IObjectProvider<IType> CoreLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Domain")
        .Or()
        .ResideInAssembly("CQRS.EntityIds")
        .Or()
        .ResideInAssembly("CQRS.Mapping")
        .Or()
        .ResideInAssembly("CQRS.DTO")
        .As("Core Layer");

    private readonly IObjectProvider<IType> ApplicationLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Application")
        .Or()
        .ResideInAssembly("CQRS.Application.WolverineHandlers")
        .As("Application Layer");

    private readonly IObjectProvider<IType> ApplicationCommandProcessingStatusRecordingLayer =
        Types()
            .That()
            .ResideInAssembly("CQRS.Application")
            .Or()
            .ResideInAssembly("CQRS.Application.CommandProcessingStatusRecording")
            .As("Application Command Processing Status Recording Layer");

    private readonly IObjectProvider<IType> ProjectionsLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Projections")
        .As("Projections Layer");

    private readonly IObjectProvider<IType> ProjectionsRepositoriesLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Projections.Repositories")
        .As("Projections Repositories Layer");

    private readonly IObjectProvider<IType> ProjectionsViewModelsLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Projections.ViewModels")
        .As("Projections ViewModels Layer");

    private readonly IObjectProvider<IType> ApiLayer = Types()
        .That()
        .ResideInAssembly("CQRS.API")
        .As("API Layer");

    private readonly IObjectProvider<IType> PortsLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Ports.EventStore")
        .Or()
        .ResideInAssembly("CQRS.Ports.MessageBus")
        .As("Ports Layer");

    private readonly IObjectProvider<IType> AdaptersLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Adapters.InMemoryEventStore")
        .Or()
        .ResideInAssembly("CQRS.Adapters.MartenDbEventStore")
        .Or()
        .ResideInAssembly("CQRS.Adapters.WolverineMessageBus")
        .As("Adapters Layer");

    private readonly IObjectProvider<IType> ApiHostLayer = Types()
        .That()
        .ResideInAssembly("CQRS.API.Host")
        .As("API Host Layer");

    private readonly IObjectProvider<IType> ApplicationHostLayer = Types()
        .That()
        .ResideInAssembly("CQRS.Application.Host")
        .As("Application Host Layer");

    private readonly IObjectProvider<IType> CliLayer = Types()
        .That()
        .ResideInAssembly("CQRS.CLI")
        .As("CLI Layer");

    private const string RuleApplicationMustNotDependOnAdapters =
        "Application MUST NOT depend on Adapters, only on Ports";

    [Fact(DisplayName = RuleApplicationMustNotDependOnAdapters)]
    public void TestRuleApplicationMustNotDependOnAdapters()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApplicationLayer)
            .Should()
            .NotDependOnAny(AdaptersLayer)
            .Because(RuleApplicationMustNotDependOnAdapters)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleProjectionsMustNotDependOnAdapters =
        "Projections MUST NOT depend on Adapters, only on Ports";

    [Fact(DisplayName = RuleProjectionsMustNotDependOnAdapters)]
    public void TestRuleProjectionsMustNotDependOnAdapters()
    {
        IArchRule rule = Types()
            .That()
            .Are(ProjectionsLayer)
            .Should()
            .NotDependOnAny(AdaptersLayer)
            .Because(RuleProjectionsMustNotDependOnAdapters)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 1 — Core MUST NOT depend on any project from Server
    // -------------------------------------------------------------------------

    private const string RuleCoreMustNotDependOnPorts = "Core MUST NOT depend on Ports";

    [Fact(DisplayName = RuleCoreMustNotDependOnPorts)]
    public void TestRuleCoreMustNotDependOnPorts()
    {
        IArchRule rule = Types()
            .That()
            .Are(CoreLayer)
            .Should()
            .NotDependOnAny(PortsLayer)
            .Because(RuleCoreMustNotDependOnPorts)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleCoreMustNotDependOnApplication = "Core MUST NOT depend on Application";

    [Fact(DisplayName = RuleCoreMustNotDependOnApplication)]
    public void TestRuleCoreMustNotDependOnApplication()
    {
        IArchRule rule = Types()
            .That()
            .Are(CoreLayer)
            .Should()
            .NotDependOnAny(ApplicationLayer)
            .Because(RuleCoreMustNotDependOnApplication)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleCoreMustNotDependOnProjections = "Core MUST NOT depend on Projections";

    [Fact(DisplayName = RuleCoreMustNotDependOnProjections)]
    public void TestRuleCoreMustNotDependOnProjections()
    {
        IArchRule rule = Types()
            .That()
            .Are(CoreLayer)
            .Should()
            .NotDependOnAny(ProjectionsLayer)
            .Because(RuleCoreMustNotDependOnProjections)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleCoreMustNotDependOnAdapters = "Core MUST NOT depend on Adapters";

    [Fact(DisplayName = RuleCoreMustNotDependOnAdapters)]
    public void TestRuleCoreMustNotDependOnAdapters()
    {
        IArchRule rule = Types()
            .That()
            .Are(CoreLayer)
            .Should()
            .NotDependOnAny(AdaptersLayer)
            .Because(RuleCoreMustNotDependOnAdapters)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleCoreMustNotDependOnApi = "Core MUST NOT depend on API";

    [Fact(DisplayName = RuleCoreMustNotDependOnApi)]
    public void TestRuleCoreMustNotDependOnApi()
    {
        IArchRule rule = Types()
            .That()
            .Are(CoreLayer)
            .Should()
            .NotDependOnAny(ApiLayer)
            .Because(RuleCoreMustNotDependOnApi)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 2.1 — Application
    // -------------------------------------------------------------------------

    private const string RuleApplicationMustNotDependOnApi = "Application MUST NOT depend on API";

    [Fact(DisplayName = RuleApplicationMustNotDependOnApi)]
    public void TestRuleApplicationMustNotDependOnApi()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApplicationLayer)
            .Should()
            .NotDependOnAny(ApiLayer)
            .Because(RuleApplicationMustNotDependOnApi)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 2.2 — Projections
    // -------------------------------------------------------------------------

    private const string RuleProjectionsMustNotDependOnApplication =
        "Projections MUST NOT depend on Application";

    [Fact(DisplayName = RuleProjectionsMustNotDependOnApplication)]
    public void TestRuleProjectionsMustNotDependOnApplication()
    {
        IArchRule rule = Types()
            .That()
            .Are(ProjectionsLayer)
            .Should()
            .NotDependOnAny(ApplicationLayer)
            .Because(RuleProjectionsMustNotDependOnApplication)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleProjectionsMustNotDependOnApi = "Projections MUST NOT depend on API";

    [Fact(DisplayName = RuleProjectionsMustNotDependOnApi)]
    public void TestRuleProjectionsMustNotDependOnApi()
    {
        IArchRule rule = Types()
            .That()
            .Are(ProjectionsLayer)
            .Should()
            .NotDependOnAny(ApiLayer)
            .Because(RuleProjectionsMustNotDependOnApi)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 2.3 — API
    // -------------------------------------------------------------------------

    private const string RuleApiMustNotDependOnAdapters =
        "API MUST NOT depend on Adapters, only on Ports";

    [Fact(DisplayName = RuleApiMustNotDependOnAdapters)]
    public void TestRuleApiMustNotDependOnAdapters()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApiLayer)
            .Should()
            .NotDependOnAny(AdaptersLayer)
            .Because(RuleApiMustNotDependOnAdapters)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 2.4 — Ports
    // -------------------------------------------------------------------------

    private const string RulePortsMustNotDependOnCore =
        "Ports MUST NOT depend on Core (Domain, DTO, Mapping)";

    [Fact(DisplayName = RulePortsMustNotDependOnCore)]
    public void TestRulePortsMustNotDependOnCore()
    {
        IArchRule rule = Types()
            .That()
            .Are(PortsLayer)
            .Should()
            .NotDependOnAny(CoreLayer)
            .Because(RulePortsMustNotDependOnCore)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RulePortsMustNotDependOnApplicationOrProjections =
        "Ports MUST NOT depend on Application or Projections";

    [Fact(DisplayName = RulePortsMustNotDependOnApplicationOrProjections)]
    public void TestRulePortsMustNotDependOnApplicationOrProjections()
    {
        IArchRule rule = Types()
            .That()
            .Are(PortsLayer)
            .Should()
            .NotDependOnAny(ApplicationLayer)
            .AndShould()
            .NotDependOnAny(ProjectionsLayer)
            .Because(RulePortsMustNotDependOnApplicationOrProjections)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RulePortsShouldNotDependOnEachOther =
        "A Port SHOULD NOT depend on another Port";

    [Fact(DisplayName = RulePortsShouldNotDependOnEachOther)]
    public void TestRulePortsShouldNotDependOnEachOther()
    {
        var eventStorePort = Types()
            .That()
            .ResideInAssembly("CQRS.Ports.EventStore")
            .As("EventStore Port");
        var messageBusPort = Types()
            .That()
            .ResideInAssembly("CQRS.Ports.MessageBus")
            .As("MessageBus Port");

        Types()
            .That()
            .Are(eventStorePort)
            .Should()
            .NotDependOnAny(messageBusPort)
            .Because(RulePortsShouldNotDependOnEachOther)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That()
            .Are(messageBusPort)
            .Should()
            .NotDependOnAny(eventStorePort)
            .Because(RulePortsShouldNotDependOnEachOther)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 2.5 — Adapters
    // -------------------------------------------------------------------------

    private const string RuleAdaptersMustNotDependOnCore =
        "Adapters MUST NOT depend on Core (Domain, DTO, Mapping)";

    [Fact(DisplayName = RuleAdaptersMustNotDependOnCore)]
    public void TestRuleAdaptersMustNotDependOnCore()
    {
        IArchRule rule = Types()
            .That()
            .Are(AdaptersLayer)
            .Should()
            .NotDependOnAny(CoreLayer)
            .Because(RuleAdaptersMustNotDependOnCore)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleAdaptersMustNotDependOnApplicationOrProjections =
        "Adapters MUST NOT depend on Application or Projections";

    [Fact(DisplayName = RuleAdaptersMustNotDependOnApplicationOrProjections)]
    public void TestRuleAdaptersMustNotDependOnApplicationOrProjections()
    {
        IArchRule rule = Types()
            .That()
            .Are(AdaptersLayer)
            .Should()
            .NotDependOnAny(ApplicationLayer)
            .AndShould()
            .NotDependOnAny(ProjectionsLayer)
            .Because(RuleAdaptersMustNotDependOnApplicationOrProjections)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 2.7 — Hosts
    // -------------------------------------------------------------------------

    private const string RuleApplicationHostMustNotDependOnApi =
        "Application.Host MUST NOT depend on API";

    [Fact(DisplayName = RuleApplicationHostMustNotDependOnApi)]
    public void TestRuleApplicationHostMustNotDependOnApi()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApplicationHostLayer)
            .Should()
            .NotDependOnAny(ApiLayer)
            .Because(RuleApplicationHostMustNotDependOnApi)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    private const string RuleApiHostMustNotDependOnApplication =
        "API.Host MUST NOT depend on Application";

    [Fact(DisplayName = RuleApiHostMustNotDependOnApplication)]
    public void TestRuleApiHostMustNotDependOnApplication()
    {
        IArchRule rule = Types()
            .That()
            .Are(ApiHostLayer)
            .Should()
            .NotDependOnAny(ApplicationLayer)
            .Because(RuleApiHostMustNotDependOnApplication)
            .WithoutRequiringPositiveResults();
        rule.Check(Architecture);
    }

    // -------------------------------------------------------------------------
    // Rule 3 — Client
    // -------------------------------------------------------------------------

    private const string RuleCliMustNotDependOnServer =
        "CLI MUST ONLY depend on CQRS.DTO and CQRS.Projections.ViewModels";

    [Fact(DisplayName = RuleCliMustNotDependOnServer)]
    public void TestRuleCliMustNotDependOnServer()
    {
        Types()
            .That()
            .Are(CliLayer)
            .Should()
            .NotDependOnAny(DomainLayer)
            .Because(RuleCliMustNotDependOnServer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That()
            .Are(CliLayer)
            .Should()
            .NotDependOnAny(ApplicationLayer)
            .Because(RuleCliMustNotDependOnServer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That()
            .Are(CliLayer)
            .Should()
            .NotDependOnAny(ProjectionsLayer)
            .Because(RuleCliMustNotDependOnServer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That()
            .Are(CliLayer)
            .Should()
            .NotDependOnAny(PortsLayer)
            .Because(RuleCliMustNotDependOnServer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That()
            .Are(CliLayer)
            .Should()
            .NotDependOnAny(AdaptersLayer)
            .Because(RuleCliMustNotDependOnServer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);

        Types()
            .That()
            .Are(CliLayer)
            .Should()
            .NotDependOnAny(ApiLayer)
            .Because(RuleCliMustNotDependOnServer)
            .WithoutRequiringPositiveResults()
            .Check(Architecture);
    }
}
