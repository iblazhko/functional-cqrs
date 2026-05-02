using CQRS.Domain.Failures;
using LanguageExt;

namespace CQRS.Mapping;

public record MappingFault : Fault
{
    public string FromType { get; init; }
    public string ToType { get; init; }

    public MappingFault(string fromType, string toType, params string[] reasons)
        : this(fromType, toType, new Seq<Error>(reasons.Select(x => new Error(x)))) { }

    public MappingFault(string fromType, string toType, Seq<Error> reasons)
        : base($"Could not map {fromType} to {toType}", reasons)
    {
        FromType = fromType;
        ToType = toType;
    }
}

public class MappingException(string fromType, string toType, params string[] reasons)
    : Exception($"Could not map {fromType} to {toType}: {string.Join(", ", reasons)}");

public static class MappingFaultExtensions
{
    public static Either<MappingFault, TTo> MapValidationFault<TFrom, TTo>(
        this Either<ValidationFault, TTo> result
    ) => result.MapLeft(x => new MappingFault(typeof(TFrom).Name, typeof(TTo).Name, x.Errors));
}
