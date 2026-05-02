using CQRS.Domain.Failures;
using CQRS.EntityIds;
using Shouldly;

namespace CQRS.EntityIds.Tests;

public sealed class EntityIdTests
{
    // 14 chars, all from the valid alphabet (no I, L, O)
    private const string ValidId = "ABCDEFGHJKMNPQ";
    private const string ValidIdLowercase = "abcdefghjkmnpq";
    private const string ValidAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ0123456789";
    private const int IdLength = 14;

    public sealed class NewId
    {
        [Fact]
        public void HasCorrectLength()
        {
            ((string)EntityId.NewId()).Length.ShouldBe(IdLength);
        }

        [Fact]
        public void UsesValidAlphabetOnly()
        {
            var id = (string)EntityId.NewId();
            foreach (var c in id)
                ValidAlphabet.ShouldContain(c);
        }

        [Fact]
        public void ProducesDifferentValuesOnSuccessiveCalls()
        {
            // With 33^14 ≈ 3e21 possible values, collision probability is negligible
            ((string)EntityId.NewId()).ShouldNotBe((string)EntityId.NewId());
        }
    }

    public sealed class Create
    {
        [Theory]
        [InlineData(ValidId)]           // 14 uppercase letters from valid alphabet
        [InlineData("00000000000000")]  // 14 digits
        [InlineData("ABCDEFGH000000")]  // letters and digits mixed
        public void WithValidId_ReturnsRight(string value)
        {
            EntityId.Create(value).IsRight.ShouldBeTrue();
        }

        [Theory]
        [InlineData(ValidIdLowercase)]  // lowercase normalises to valid uppercase
        [InlineData("Abcdefghjkmnpq")]  // mixed case
        public void WithLowercaseValidId_ReturnsRight(string value)
        {
            EntityId.Create(value).IsRight.ShouldBeTrue();
        }

        [Fact]
        public void NormalisesInputToUppercase()
        {
            EntityId.Create(ValidIdLowercase).Match(
                Left: _ => Assert.Fail("Expected Right"),
                Right: id => ((string)id).ShouldBe(ValidId)
            );
        }

        [Theory]
        [InlineData("")]                 // empty
        [InlineData("ABCDEFGHJKMNP")]   // 13 chars — too short
        [InlineData("ABCDEFGHJKMNPQR")] // 15 chars — too long
        public void WithWrongLength_ReturnsLeft(string value)
        {
            EntityId.Create(value).IsLeft.ShouldBeTrue();
        }

        [Theory]
        [InlineData("ABCDEFGHIJKMNP")] // contains 'I' (excluded for legibility)
        [InlineData("ABCDEFGHJKLMNP")] // contains 'L' (excluded for legibility)
        [InlineData("ABCDEFGHJKOMNP")] // contains 'O' (excluded for legibility)
        public void WithExcludedLetter_ReturnsLeft(string value)
        {
            EntityId.Create(value).IsLeft.ShouldBeTrue();
        }

        [Theory]
        [InlineData("ABCDEFGH!JKMNP")] // '!'
        [InlineData("ABCDEFGH JKMNP")] // space
        [InlineData("ABCDEFGH-JKMNP")] // '-'
        public void WithInvalidCharacter_ReturnsLeft(string value)
        {
            EntityId.Create(value).IsLeft.ShouldBeTrue();
        }

        [Fact]
        public void WhenInvalid_ReturnsValidationFault()
        {
            var result = EntityId.Create("TOOSHORT");
            result.IsLeft.ShouldBeTrue();
            result.IfLeft(fault => fault.ShouldBeOfType<ValidationFault>());
        }
    }

    public sealed class CreateUnsafe
    {
        [Fact]
        public void WithValidId_ReturnsInstance()
        {
            ((string)EntityId.CreateUnsafe(ValidId)).ShouldBe(ValidId);
        }

        [Fact]
        public void WithLowercaseValidId_NormalisesAndReturnsInstance()
        {
            ((string)EntityId.CreateUnsafe(ValidIdLowercase)).ShouldBe(ValidId);
        }

        [Theory]
        [InlineData("ABCDEFGHJKMNP")]   // too short
        [InlineData("ABCDEFGHJKMNPQR")] // too long
        [InlineData("ABCDEFGHIJKMNP")]  // contains 'I'
        [InlineData("ABCDEFGH!JKMNP")]  // invalid character
        public void WithInvalidId_ThrowsArgumentException(string value)
        {
            Should.Throw<ArgumentException>(() => EntityId.CreateUnsafe(value));
        }
    }

    public sealed class StringConversion
    {
        [Fact]
        public void ImplicitCastToString_ReturnsUnderlyingValue()
        {
            var id = EntityId.CreateUnsafe(ValidId);
            string s = id;
            s.ShouldBe(ValidId);
        }

        [Fact]
        public void ToString_ReturnsUnderlyingValue()
        {
            EntityId.CreateUnsafe(ValidId).ToString().ShouldBe(ValidId);
        }
    }
}
