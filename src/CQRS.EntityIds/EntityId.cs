using System.Text.RegularExpressions;
using CQRS.Domain.Failures;
using LanguageExt;
using NanoidDotNet;

namespace CQRS.EntityIds;

public readonly record struct EntityId
{
    private string Value { get; }

    private EntityId(string value)
    {
        Value = value;
    }

    public static implicit operator string(EntityId id) => id.Value;

    public override string ToString() => Value;

    private const string IdAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ0123456789"; // letters I, L, O excluded for legibility (could be confused with digits 0, 1)
    private const int IdLength = 14;

    // As per https://zelark.github.io/nano-id-cc/
    // with the alphabet and length above at 10 IDs / second rate
    // ~19 years or 6B IDs needed in order to have a 1% probability of at least one collision

    public static EntityId NewId() => new(Nanoid.Generate(IdAlphabet, IdLength));

    public static Either<ValidationFault, EntityId> Create(string value) =>
        value.ToUpperInvariant() switch
        {
            var x when !IsValidId(x) => new ValidationFault(
                InvalidIdMessage,
                [new Error($"Expected {IdLength} alphanumeric characters, got '{x}'")]
            ),
            var x => new EntityId(x),
        };

    internal static EntityId CreateUnsafe(string value) =>
        value.ToUpperInvariant() switch
        {
            var x when !IsValidId(x) => throw new ArgumentException(
                InvalidIdMessage,
                nameof(value)
            ),
            var x => new EntityId(x),
        };

    private static bool IsValidId(string id) => IdFormatRegex.IsMatch(id);

    private static readonly Regex IdFormatRegex = new(
        $"^[{IdAlphabet}]{{{IdLength}}}$",
        RegexOptions.Compiled
    );

    private static string InvalidIdMessage =>
        $"Entity Id must be {IdLength} alphanumeric characters from set '{IdAlphabet}'";
}
