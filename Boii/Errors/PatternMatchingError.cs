using System;

namespace Boii.Errors;

public class PatternMatchingError<TUnion> : Exception
{
    public TUnion Variant { get; }

    private PatternMatchingError(TUnion variant) : base($"Non exhaustive pattern matching. {variant} is not handled for {typeof(TUnion).FullName}") =>
        Variant = variant;

    public static PatternMatchingError<TUnion> Create(TUnion variant) => new(variant);
}

public static class PatternMatchingError
{
    public static PatternMatchingError<TUnion> Create<TUnion>(TUnion variant) => PatternMatchingError<TUnion>.Create(variant);
}
