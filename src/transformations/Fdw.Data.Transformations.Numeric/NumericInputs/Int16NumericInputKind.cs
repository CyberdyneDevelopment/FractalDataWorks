using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A 16-bit whole number.</summary>
[TypeOption(typeof(NumericInputKinds), "Int16")]
[ExcludeFromCodeCoverage]
public sealed class Int16NumericInputKind : NumericInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int16NumericInputKind"/> class.</summary>
    public Int16NumericInputKind()
        : base("Int16")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is short;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal((short)value);
}
