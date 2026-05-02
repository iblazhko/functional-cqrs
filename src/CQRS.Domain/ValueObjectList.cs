using System.Collections;
using System.Collections.Immutable;

namespace CQRS.Domain;

public class ValueObjectsList<T>(IImmutableList<T> valuesList)
    : IImmutableList<T>,
        IEquatable<IImmutableList<T>>
{
    #region IImmutableList implementation - delegate to the wrapped valuesList

    public T this[int index] => valuesList[index];

    public int Count => valuesList.Count;

    public IImmutableList<T> Add(T value) => valuesList.Add(value).WithValueSemantic();

    public IImmutableList<T> AddRange(IEnumerable<T> items) =>
        valuesList.AddRange(items).WithValueSemantic();

    public IImmutableList<T> Clear() => valuesList.Clear().WithValueSemantic();

    public IEnumerator<T> GetEnumerator() => valuesList.GetEnumerator();

    public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) =>
        valuesList.IndexOf(item, index, count, equalityComparer);

    public IImmutableList<T> Insert(int index, T element) =>
        valuesList.Insert(index, element).WithValueSemantic();

    public IImmutableList<T> InsertRange(int index, IEnumerable<T> items) =>
        valuesList.InsertRange(index, items).WithValueSemantic();

    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) =>
        valuesList.LastIndexOf(item, index, count, equalityComparer);

    public IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer) =>
        valuesList.Remove(value, equalityComparer).WithValueSemantic();

    public IImmutableList<T> RemoveAll(Predicate<T> match) =>
        valuesList.RemoveAll(match).WithValueSemantic();

    public IImmutableList<T> RemoveAt(int index) => valuesList.RemoveAt(index).WithValueSemantic();

    public IImmutableList<T> RemoveRange(
        IEnumerable<T> items,
        IEqualityComparer<T>? equalityComparer
    ) => valuesList.RemoveRange(items, equalityComparer).WithValueSemantic();

    public IImmutableList<T> RemoveRange(int index, int count) =>
        valuesList.RemoveRange(index, count).WithValueSemantic();

    public IImmutableList<T> Replace(
        T oldValue,
        T newValue,
        IEqualityComparer<T>? equalityComparer
    ) => valuesList.Replace(oldValue, newValue, equalityComparer).WithValueSemantic();

    public IImmutableList<T> SetItem(int index, T value) => valuesList.SetItem(index, value);

    IEnumerator IEnumerable.GetEnumerator() => valuesList.GetEnumerator();

    #endregion

    public override bool Equals(object? obj) => Equals(obj as IImmutableList<T>);

    public bool Equals(IImmutableList<T>? other) =>
        this.SequenceEqual(other ?? ImmutableList<T>.Empty);

    public override int GetHashCode()
    {
        unchecked
        {
            return this.Aggregate(977, (h, x) => h * 293 + (x?.GetHashCode() ?? 0));
        }
    }
}

public static class ValueObjectsListExtensions
{
    public static IImmutableList<T> WithValueSemantic<T>(this IImmutableList<T>? list) =>
        new ValueObjectsList<T>(list ?? []);

    public static IImmutableList<T> AsListWithValueSemantic<T>(this IEnumerable<T>? list) =>
        (list ?? []).ToImmutableList().WithValueSemantic();
}
