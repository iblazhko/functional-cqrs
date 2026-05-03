using CQRS.API.Inventory;
using CQRS.Mapping;
using LanguageExt;
using static LanguageExt.Prelude;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;

namespace CQRS.API.Tests;

public sealed class InventoriesApiHttpExtensionsTests
{
    private static MappingFault SomeFault() =>
        new("SourceType", "DestType", "field is invalid");

    private static AcceptedResponse SomeAccepted() =>
        new() { InventoryId = "INV001", CorrelationId = Guid.NewGuid() };

    private static InventoryResponse SomeResponse() =>
        new() { InventoryId = "INV001", Name = "Widget", StockQuantity = 5, IsActive = true };

    // --- Option<T>.ToHttpResult() ---

    [Fact]
    public void ToHttpResult_OptionSome_ReturnsOk()
    {
        Option<InventoryResponse> option = Some(SomeResponse());

        var result = option.ToHttpResult();

        result.ShouldBeOfType<Ok<InventoryResponse>>();
    }

    [Fact]
    public void ToHttpResult_OptionSome_OkContainsValue()
    {
        var response = SomeResponse();
        Option<InventoryResponse> option = Some(response);

        var result = (Ok<InventoryResponse>)option.ToHttpResult();

        result.Value.ShouldBe(response);
    }

    [Fact]
    public void ToHttpResult_OptionNone_ReturnsNotFound()
    {
        Option<InventoryResponse> option = None;

        var result = option.ToHttpResult();

        result.ShouldBeOfType<NotFound>();
    }

    // --- Either<MappingFault, AcceptedResponse>.ToHttpResult() ---

    [Fact]
    public void ToHttpResult_EitherRight_ReturnsAccepted()
    {
        Either<MappingFault, AcceptedResponse> either = SomeAccepted();

        var result = either.ToHttpResult();

        result.ShouldBeOfType<Accepted<AcceptedResponse>>();
    }

    [Fact]
    public void ToHttpResult_EitherRight_AcceptedContainsValue()
    {
        var accepted = SomeAccepted();
        Either<MappingFault, AcceptedResponse> either = accepted;

        var result = (Accepted<AcceptedResponse>)either.ToHttpResult();

        result.Value.ShouldBe(accepted);
    }

    [Fact]
    public void ToHttpResult_EitherLeft_ReturnsBadRequest()
    {
        var fault = SomeFault();
        Either<MappingFault, AcceptedResponse> either = fault;

        var result = (ProblemHttpResult)either.ToHttpResult();

        result.ShouldBeOfType<ProblemHttpResult>();
    }

    [Fact]
    public void ToHttpResult_EitherLeft_BadRequestContainsFault()
    {
        var fault = SomeFault();
        Either<MappingFault, AcceptedResponse> either = fault;

        var result = (ProblemHttpResult)either.ToHttpResult();

        result.ProblemDetails.Detail.ShouldBe(fault.Message);
        result.ProblemDetails.Extensions.ShouldNotBeNull();
        result.ProblemDetails.Extensions.ContainsKey("errors").ShouldBeTrue();
        result.ProblemDetails.Extensions["errors"].ShouldBe(fault.Errors.Select(e => e.Message).ToArray());
    }
}
