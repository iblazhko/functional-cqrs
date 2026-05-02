using CQRS.Application.Inventory;
using CQRS.Domain.Inventory;
using CQRS.Mapping;
using LanguageExt;
using Shouldly;

namespace CQRS.Application.Tests;

public sealed class CommandProcessingResultTests
{
    private static Seq<IInventoryEvent> SomeEvents() =>
        new InventoryCreated(
            InventoryId.NewId(),
            InventoryName.CreateUnsafe("Widget"),
            true
        ).ToSeq();

    private static MappingFault SomeFault() =>
        new("SourceType", "DestType", "field is invalid");

    private static InventoryAggregate.Errors.IInventoryCommandError SomeError() =>
        new InventoryAggregate.Errors.InventoryAlreadyExists(InventoryId.NewId());

    [Fact]
    public void Completed_IsFirst()
    {
        var result = CommandProcessingResult.Completed(SomeEvents());

        result.IsT0.ShouldBeTrue();
    }

    [Fact]
    public void Completed_PreservesEvents()
    {
        var events = SomeEvents();

        var result = CommandProcessingResult.Completed(events);

        result.AsT0.NewEvents.ShouldBe(events);
    }

    [Fact]
    public void Rejected_IsSecond()
    {
        var result = CommandProcessingResult.Rejected(SomeFault());

        result.IsT1.ShouldBeTrue();
    }

    [Fact]
    public void Rejected_PreservesFault()
    {
        var fault = SomeFault();

        var result = CommandProcessingResult.Rejected(fault);

        result.AsT1.Fault.ShouldBe(fault);
    }

    [Fact]
    public void Failed_IsThird()
    {
        var result = CommandProcessingResult.Failed(SomeError());

        result.IsT2.ShouldBeTrue();
    }

    [Fact]
    public void Failed_PreservesError()
    {
        var error = SomeError();

        var result = CommandProcessingResult.Failed(error);

        result.AsT2.Error.ShouldBe(error);
    }
}
