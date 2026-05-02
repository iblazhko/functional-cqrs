using CQRS.Domain.Inventory;
using LanguageExt;
using Shouldly;

namespace CQRS.Domain.Tests;

public static class DomainAggregateResultExtensions
{
    public static void ShouldProduceEvents(
        this Either<
            InventoryAggregate.Errors.IInventoryCommandError,
            Seq<IInventoryEvent>
        > aggregateResult,
        Seq<IInventoryEvent> expectedEvents
    )
    {
        aggregateResult.Match(
            failure =>
            {
                Assert.Fail($"Aggregate operation failed: {failure.GetType().FullName}.");
            },
            events =>
            {
                events.ToArray().ShouldBe(expectedEvents.ToArray());
            }
        );
    }

    public static void ShouldProduceFailure(
        this Either<
            InventoryAggregate.Errors.IInventoryCommandError,
            Seq<IInventoryEvent>
        > aggregateResult,
        InventoryAggregate.Errors.IInventoryCommandError expectedFailure
    )
    {
        aggregateResult.Match(
            failure =>
            {
                failure.ShouldBe(expectedFailure);
            },
            _ =>
            {
                Assert.Fail(
                    $"Aggregate should have rejected command with error: {expectedFailure.GetType().FullName}."
                );
            }
        );
    }
}
