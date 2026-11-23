using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A double.</summary>
[TypeOption(typeof(NumericInputKinds), "Double")]
[ExcludeFromCodeCoverage]
public sealed class DoubleNumericInputKind : NumericInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DoubleNumericInputKind"/> class.</summary>
    public DoubleNumericInputKind()
        : base("Double")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is double;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal((double)value);
}
