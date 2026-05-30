using CQRS.Domain.Failures;
using LanguageExt;
using Shouldly;

namespace CQRS.Mapping.Tests;

public sealed class MappingFaultTests
{
    [Fact]
    public void Constructor_WithStringReasons_SetsFromTypeAndToType()
    {
        var fault = new MappingFault("SourceType", "TargetType", "something went wrong");

        fault.FromType.ShouldBe("SourceType");
        fault.ToType.ShouldBe("TargetType");
    }

    [Fact]
    public void Constructor_WithStringReasons_MessageContainsTypeNames()
    {
        var fault = new MappingFault("SourceType", "TargetType", "reason");

        fault.Message.ShouldContain("SourceType");
        fault.Message.ShouldContain("TargetType");
    }

    [Fact]
    public void Constructor_WithSeqErrors_SetsFromTypeAndToType()
    {
        var errors = new Seq<Error>([new Error("err1"), new Error("err2")]);
        var fault = new MappingFault("From", "To", errors);

        fault.FromType.ShouldBe("From");
        fault.ToType.ShouldBe("To");
        fault.Errors.Count.ShouldBe(2);
    }
}

public sealed class MappingExceptionTests
{
    [Fact]
    public void Constructor_MessageContainsBothTypeNamesAndReasons()
    {
        var ex = new MappingException("TypeA", "TypeB", "reason 1", "reason 2");

        ex.Message.ShouldContain("TypeA");
        ex.Message.ShouldContain("TypeB");
        ex.Message.ShouldContain("reason 1");
        ex.Message.ShouldContain("reason 2");
    }
}

public sealed class MapValidationFaultExtensionTests
{
    [Fact]
    public void MapValidationFault_WhenLeft_ReturnsMappingFaultWithCorrectTypeNames()
    {
        Either<ValidationFault, string> validationResult = new ValidationFault(
            "bad value",
            [new Error("must not be empty")]
        );

        var result = validationResult.MapValidationFault<int, string>();

        result.IsLeft.ShouldBeTrue();
        result.Match(
            Left: f =>
            {
                f.FromType.ShouldBe(nameof(Int32));
                f.ToType.ShouldBe(nameof(String));
            },
            Right: _ => Assert.Fail("Expected Left")
        );
    }

    [Fact]
    public void MapValidationFault_WhenRight_PassesThroughUnchanged()
    {
        Either<ValidationFault, string> validationResult = "success";

        var result = validationResult.MapValidationFault<int, string>();

        result.IsRight.ShouldBeTrue();
        result.Match(Left: _ => Assert.Fail("Expected Right"), Right: v => v.ShouldBe("success"));
    }

    [Fact]
    public void MapValidationFault_WhenLeft_PreservesErrors()
    {
        var error = new Error("field is required");
        Either<ValidationFault, string> validationResult = new ValidationFault(
            "validation failed",
            [error]
        );

        var result = validationResult.MapValidationFault<int, string>();

        result.Match(
            Left: f => f.Errors.ShouldContain(error),
            Right: _ => Assert.Fail("Expected Left")
        );
    }
}
