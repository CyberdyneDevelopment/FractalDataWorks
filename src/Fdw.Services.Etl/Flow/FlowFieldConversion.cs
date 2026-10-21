using System;
using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// What an input mapping does to its value besides copying it (FDW-2000): the default a null value takes, the chain of named Transformations the
/// value goes through, the type it is converted to, and whether it must have a value.
/// </summary>
/// <remarks>
/// Applied in this order: a null value takes the default; a value that is still null is refused when the mapping is required and otherwise stays
/// null; any other value goes through the chain and is then converted. Each part is stated or absent; none is assumed.
/// </remarks>
public sealed class FlowFieldConversion
{
    /// <summary>Initializes a new instance of the <see cref="FlowFieldConversion"/> class.</summary>
    /// <param name="targetType">The type name the value is converted to; null when it is not converted.</param>
    /// <param name="defaultValue">What a null value takes, already read as text; null when a null value stays null.</param>
    /// <param name="isRequired">Whether a value that is still null is refused.</param>
    /// <param name="chain">The Transformations the value goes through, in order; empty when it goes through none.</param>
    public FlowFieldConversion(string? targetType, string? defaultValue, bool isRequired, IReadOnlyList<IFieldMappingTransformation> chain)
    {
        ArgumentNullException.ThrowIfNull(chain);
        TargetType = targetType;
        DefaultValue = defaultValue;
        IsRequired = isRequired;
        Chain = chain;
    }

    /// <summary>Gets the type name the value is converted to; null when it is not converted.</summary>
    public string? TargetType { get; }

    /// <summary>Gets what a null value takes; null when a null value stays null.</summary>
    public string? DefaultValue { get; }

    /// <summary>Gets whether a value that is still null is refused.</summary>
    public bool IsRequired { get; }

    /// <summary>Gets the Transformations the value goes through, in order.</summary>
    public IReadOnlyList<IFieldMappingTransformation> Chain { get; }
}
