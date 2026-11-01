using System;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// One field of a component's output row and where its value comes from: a field of the incoming row or a constant (FDW-1386).
/// </summary>
/// <remarks>
/// A mapping names exactly one of the two. A mapped field the incoming row does not have is a failure, never a null. A mapping may also carry a
/// <see cref="FlowFieldConversion"/>: a default, a chain of Transformations, a type to convert to, and whether the value is required (FDW-2000).
/// </remarks>
public sealed class FlowFieldMap
{
    private FlowFieldMap(string target, string? source, object? constant, bool isConstant, FlowFieldConversion? conversion = null)
    {
        Target = target;
        Source = source;
        Constant = constant;
        IsConstant = isConstant;
        Conversion = conversion;
    }

    /// <summary>Gets the name of the field in the output row.</summary>
    public string Target { get; }

    /// <summary>Gets the name of the incoming field the value is copied from; null for a constant.</summary>
    public string? Source { get; }

    /// <summary>Gets the constant value; meaningful only when <see cref="IsConstant"/>.</summary>
    public object? Constant { get; }

    /// <summary>Gets whether the value is a constant.</summary>
    public bool IsConstant { get; }

    /// <summary>Gets what the mapping does to its value besides copying it; null when it does nothing.</summary>
    public FlowFieldConversion? Conversion { get; }

    /// <summary>Makes the same mapping with a conversion.</summary>
    /// <param name="conversion">What the mapping does to its value.</param>
    /// <returns>The mapping.</returns>
    public FlowFieldMap WithConversion(FlowFieldConversion conversion)
    {
        ArgumentNullException.ThrowIfNull(conversion);
        return new FlowFieldMap(Target, Source, Constant, IsConstant, conversion);
    }

    /// <summary>Maps an incoming field to an output field.</summary>
    /// <param name="target">The output field.</param>
    /// <param name="source">The incoming field.</param>
    /// <returns>The mapping.</returns>
    public static FlowFieldMap From(string target, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new FlowFieldMap(target, source, null, false);
    }

    /// <summary>Gives an output field a constant value.</summary>
    /// <param name="target">The output field.</param>
    /// <param name="value">The value, which may be null when null is what is meant.</param>
    /// <returns>The mapping.</returns>
    public static FlowFieldMap Of(string target, object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        return new FlowFieldMap(target, null, value, true);
    }
}
