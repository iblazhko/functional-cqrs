using System.Collections.Immutable;
using Shouldly;

namespace CQRS.Domain.Tests;

public sealed class ValueObjectsListTests
{
    [Fact]
    public void TwoListsWithSameElements_AreEqual()
    {
        var a = new[] { 1, 2, 3 }.AsListWithValueSemantic();
        var b = new[] { 1, 2, 3 }.AsListWithValueSemantic();

        a.ShouldBe(b);
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void TwoListsWithDifferentElements_AreNotEqual()
    {
        var a = new[] { 1, 2, 3 }.AsListWithValueSemantic();
        var b = new[] { 1, 2, 4 }.AsListWithValueSemantic();

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void TwoEmptyLists_AreEqual()
    {
        var a = Array.Empty<int>().AsListWithValueSemantic();
        var b = Array.Empty<int>().AsListWithValueSemantic();

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ListsWithDifferentLengths_AreNotEqual()
    {
        var a = new[] { 1, 2 }.AsListWithValueSemantic();
        var b = new[] { 1, 2, 3 }.AsListWithValueSemantic();

        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void EqualLists_HaveSameHashCode()
    {
        var a = new[] { 1, 2, 3 }.AsListWithValueSemantic();
        var b = new[] { 1, 2, 3 }.AsListWithValueSemantic();

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        var a = new[] { 1 }.AsListWithValueSemantic();

        a.Equals((IImmutableList<int>?)null).ShouldBeFalse();
    }

    [Fact]
    public void WithValueSemantic_WrapsExistingImmutableList()
    {
        var source = ImmutableList.Create(1, 2, 3);
        var wrapped = source.WithValueSemantic();

        wrapped.ShouldBeOfType<ValueObjectsList<int>>();
        wrapped.Count.ShouldBe(3);
    }

    [Fact]
    public void WithValueSemantic_WithNull_ReturnsEmptyList()
    {
        var wrapped = ((IImmutableList<int>?)null).WithValueSemantic();

        wrapped.ShouldBeOfType<ValueObjectsList<int>>();
        wrapped.Count.ShouldBe(0);
    }

    [Fact]
    public void AsListWithValueSemantic_FromEnumerable_ReturnsValueObjectsList()
    {
        var result = new[] { "a", "b" }.AsListWithValueSemantic();

        result.ShouldBeOfType<ValueObjectsList<string>>();
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void AsListWithValueSemantic_WithNull_ReturnsEmptyList()
    {
        var result = ((IEnumerable<int>?)null).AsListWithValueSemantic();

        result.ShouldBeOfType<ValueObjectsList<int>>();
        result.Count.ShouldBe(0);
    }
}
