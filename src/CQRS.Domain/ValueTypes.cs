using CQRS.Domain.Failures;

namespace CQRS.Domain;

// Building blocks for domain model.
// These value types are domain-specific, but not entity-specific.

public sealed record PositiveInteger : IComparable<int>, IComparable<PositiveInteger>
{
    private int Value { get; }

    private PositiveInteger(int value)
    {
        Value = value;
    }

    public static Either<ValidationFault, PositiveInteger> Create(int value) =>
        value <= 0
            ? new ValidationFault(
                "Value must be a positive integer",
                [new Error($"Expected value greater than 0, got {value}")]
            )
            : new PositiveInteger(value);

    internal static PositiveInteger CreateUnsafe(int value) =>
        value <= 0
            // This "throw" is a defencive mechanism added for the sake of completeness. This branch is not reached in practice.
            ? throw new ArgumentOutOfRangeException(
                nameof(value),
                "Value must be a positive integer"
            )
            : new PositiveInteger(value);

    public static implicit operator int(PositiveInteger value) => value.Value;

    public static PositiveInteger operator +(PositiveInteger lhs, PositiveInteger rhs) =>
        new(lhs.Value + rhs.Value);

    public static PositiveInteger operator +(PositiveInteger lhs, Option<PositiveInteger> rhs) =>
        rhs.Match(x => lhs + x, () => lhs);

    public static PositiveInteger operator +(Option<PositiveInteger> lhs, PositiveInteger rhs) =>
        rhs + lhs;

    public int CompareTo(PositiveInteger? other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        if (other is null)
            return 1;
        return Value.CompareTo(other.Value);
    }

    public int CompareTo(int other) => Value.CompareTo(other);
}

public sealed record ShortString
{
    private string Value { get; }

    private ShortString(string value)
    {
        Value = value;
    }

    public static implicit operator string(ShortString value) => value.Value;

    public override string ToString() => Value;

    private const int MaxLength = 10;

    public static Either<ValidationFault, ShortString> Create(string value) =>
        value switch
        {
            _ when string.IsNullOrWhiteSpace(value) => new ValidationFault(
                "Value must be a non-empty string",
                [new Error($"Expected non-empty value, got {value}")]
            ),
            { Length: > MaxLength } => new ValidationFault(
                $"Value must be below {MaxLength} characters",
                [new Error($"Expected below {MaxLength} characters, got {value.Length} characters")]
            ),
            _ => new ShortString(value),
        };
}

public sealed record MediumString
{
    private string Value { get; }

    private MediumString(string value)
    {
        Value = value;
    }

    public static implicit operator string(MediumString value) => value.Value;

    public override string ToString() => Value;

    private const int MaxLength = 50;

    public static Either<ValidationFault, MediumString> Create(string value) =>
        value switch
        {
            _ when string.IsNullOrWhiteSpace(value) => new ValidationFault(
                "Value must be a non-empty string",
                [new Error($"Expected non-empty value, got '{value}'")]
            ),
            { Length: > MaxLength } => new ValidationFault(
                $"Value must be below {MaxLength} characters",
                [new Error($"Expected below {MaxLength} characters, got {value.Length} characters")]
            ),
            _ => new MediumString(value),
        };

    internal static MediumString CreateUnsafe(string value) =>
        value switch
        {
            _ when string.IsNullOrWhiteSpace(value) || value.Length > MaxLength =>
                throw new ArgumentException(
                    $"Value must be non-empty string up to {MaxLength} characters in length",
                    nameof(value)
                ),
            _ => new MediumString(value),
        };
}

public sealed record LongString
{
    private string Value { get; }

    private LongString(string value)
    {
        Value = value;
    }

    public static implicit operator string(LongString value) => value.Value;

    public override string ToString() => Value;

    private const int MaxLength = 1000;

    public static Either<ValidationFault, LongString> Create(string value) =>
        value switch
        {
            _ when string.IsNullOrWhiteSpace(value) => new ValidationFault(
                "Value must be a non-empty string",
                [new Error($"Expected non-empty value, got {value}")]
            ),
            { Length: > MaxLength } => new ValidationFault(
                $"Value must be below {MaxLength} characters",
                [new Error($"Expected below {MaxLength} characters, got {value.Length} characters")]
            ),
            _ => new LongString(value),
        };
}

public sealed record TimeZone
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    private string Value { get; }

    private TimeZone(string value)
    {
        Value = value;
    }

    public static implicit operator string(TimeZone value) => value.Value;

    public override string ToString() => Value;

    public static Either<ValidationFault, TimeZone> Create(string value) =>
        value switch
        {
            _ when string.IsNullOrWhiteSpace(value) => new ValidationFault(
                "Value must be a non-empty string representing IANA timezone identifier",
                [new Error($"Expected non-empty value, got '{value}'")]
            ),
            // TODO: consider validation to ensure the value is a known TZ identifier
            _ => new TimeZone(value),
        };

    internal static TimeZone CreateUnsafe(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException(
                "Value must be a non-empty string representing IANA timezone identifier",
                nameof(value)
            )
            : new TimeZone(value);
}
