using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A 64-bit whole number.</summary>
[TypeOption(typeof(NumericInputKinds), "Int64")]
[ExcludeFromCodeCoverage]
public sealed class Int64NumericInputKind : NumericInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int64NumericInputKind"/> class.</summary>
    public Int64NumericInputKind()
        : base("Int64")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is long;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal((long)value);
}
