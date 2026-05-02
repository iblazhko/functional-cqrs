using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace CQRS.Domain.Tests;

public sealed class PositiveIntegerTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Create_WithValidValue_ReturnsRight(int value)
    {
        PositiveInteger.Create(value).IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNonPositiveValue_ReturnsLeft(int value)
    {
        PositiveInteger.Create(value).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void CreateUnsafe_WithValidValue_ReturnsInstance()
    {
        ((int)PositiveInteger.CreateUnsafe(5)).ShouldBe(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateUnsafe_WithNonPositiveValue_Throws(int value)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PositiveInteger.CreateUnsafe(value));
    }

    [Fact]
    public void ImplicitIntConversion_ReturnsUnderlyingValue()
    {
        var pi = PositiveInteger.Create(7);
        pi.Match(_ => Assert.Fail("Expected Right"), v => ((int)v).ShouldBe(7));
    }

    [Fact]
    public void Addition_TwoPositiveIntegers_ReturnsSummedValue()
    {
        var a = PositiveInteger.CreateUnsafe(3);
        var b = PositiveInteger.CreateUnsafe(4);
        ((int)(a + b)).ShouldBe(7);
    }

    [Fact]
    public void Addition_WithOptionSome_ReturnsSummedValue()
    {
        var a = PositiveInteger.CreateUnsafe(3);
        var b = Some(PositiveInteger.CreateUnsafe(4));
        ((int)(a + b)).ShouldBe(7);
    }

    [Fact]
    public void Addition_WithOptionNone_ReturnsUnchangedValue()
    {
        var a = PositiveInteger.CreateUnsafe(3);
        Option<PositiveInteger> b = None;
        ((int)(a + b)).ShouldBe(3);
    }

    [Fact]
    public void Addition_OptionPlusPositiveInteger_IsCommutative()
    {
        var a = Some(PositiveInteger.CreateUnsafe(3));
        var b = PositiveInteger.CreateUnsafe(4);
        ((int)(a + b)).ShouldBe(7);
    }

    [Fact]
    public void CompareTo_Int_ReturnsCorrectOrder()
    {
        var pi = PositiveInteger.CreateUnsafe(5);
        pi.CompareTo(3).ShouldBeGreaterThan(0);
        pi.CompareTo(5).ShouldBe(0);
        pi.CompareTo(7).ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_PositiveInteger_ReturnsCorrectOrder()
    {
        var pi = PositiveInteger.CreateUnsafe(5);
        pi.CompareTo(PositiveInteger.CreateUnsafe(3)).ShouldBeGreaterThan(0);
        pi.CompareTo(PositiveInteger.CreateUnsafe(5)).ShouldBe(0);
        pi.CompareTo(PositiveInteger.CreateUnsafe(7)).ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_Null_ReturnsPositive()
    {
        PositiveInteger.CreateUnsafe(1).CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_Self_ReturnsZero()
    {
        var pi = PositiveInteger.CreateUnsafe(5);
        pi.CompareTo(pi).ShouldBe(0);
    }
}

public sealed class ShortStringTests
{
    [Fact]
    public void Create_WithValidValue_ReturnsRight()
    {
        ShortString.Create("Valid").IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ReturnsLeft(string value)
    {
        ShortString.Create(value).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValueExceedingMaxLength_ReturnsLeft()
    {
        ShortString.Create("12345678901").IsLeft.ShouldBeTrue(); // 11 chars, max 10
    }

    [Fact]
    public void Create_WithValueAtMaxLength_ReturnsRight()
    {
        ShortString.Create("1234567890").IsRight.ShouldBeTrue(); // exactly 10 chars
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsUnderlyingValue()
    {
        ShortString.Create("Hello").Match(
            _ => Assert.Fail("Expected Right"),
            s => ((string)s).ShouldBe("Hello")
        );
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        ShortString.Create("Hello").Match(
            _ => Assert.Fail("Expected Right"),
            s => s.ToString().ShouldBe("Hello")
        );
    }
}

public sealed class MediumStringTests
{
    [Fact]
    public void Create_WithValidValue_ReturnsRight()
    {
        MediumString.Create("Valid medium string").IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ReturnsLeft(string value)
    {
        MediumString.Create(value).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValueExceedingMaxLength_ReturnsLeft()
    {
        MediumString.Create(new string('x', 51)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValueAtMaxLength_ReturnsRight()
    {
        MediumString.Create(new string('x', 50)).IsRight.ShouldBeTrue();
    }

    [Fact]
    public void CreateUnsafe_WithValidValue_ReturnsInstance()
    {
        ((string)MediumString.CreateUnsafe("Hello")).ShouldBe("Hello");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateUnsafe_WithEmptyOrWhitespace_Throws(string value)
    {
        Should.Throw<ArgumentException>(() => MediumString.CreateUnsafe(value));
    }

    [Fact]
    public void CreateUnsafe_WithValueExceedingMaxLength_Throws()
    {
        Should.Throw<ArgumentException>(() => MediumString.CreateUnsafe(new string('x', 51)));
    }
}

public sealed class LongStringTests
{
    [Fact]
    public void Create_WithValidValue_ReturnsRight()
    {
        LongString.Create("Valid long string").IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ReturnsLeft(string value)
    {
        LongString.Create(value).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValueExceedingMaxLength_ReturnsLeft()
    {
        LongString.Create(new string('x', 1001)).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithValueAtMaxLength_ReturnsRight()
    {
        LongString.Create(new string('x', 1000)).IsRight.ShouldBeTrue();
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsUnderlyingValue()
    {
        LongString.Create("Hello").Match(
            _ => Assert.Fail("Expected Right"),
            s => ((string)s).ShouldBe("Hello")
        );
    }
}

public sealed class TimeZoneTests
{
    [Fact]
    public void Create_WithValidValue_ReturnsRight()
    {
        CQRS.Domain.TimeZone.Create("Europe/London").IsRight.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ReturnsLeft(string value)
    {
        CQRS.Domain.TimeZone.Create(value).IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void CreateUnsafe_WithValidValue_ReturnsInstance()
    {
        ((string)CQRS.Domain.TimeZone.CreateUnsafe("America/New_York")).ShouldBe("America/New_York");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateUnsafe_WithEmptyOrWhitespace_Throws(string value)
    {
        Should.Throw<ArgumentException>(() => CQRS.Domain.TimeZone.CreateUnsafe(value));
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsUnderlyingValue()
    {
        CQRS.Domain.TimeZone.Create("Asia/Tokyo").Match(
            _ => Assert.Fail("Expected Right"),
            tz => ((string)tz).ShouldBe("Asia/Tokyo")
        );
    }

    [Fact]
    public void ToString_ReturnsUnderlyingValue()
    {
        CQRS.Domain.TimeZone.Create("Asia/Tokyo").Match(
            _ => Assert.Fail("Expected Right"),
            tz => tz.ToString().ShouldBe("Asia/Tokyo")
        );
    }
}
