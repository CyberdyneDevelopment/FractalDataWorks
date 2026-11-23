using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A 32-bit whole number.</summary>
[TypeOption(typeof(NumericInputKinds), "Int32")]
[ExcludeFromCodeCoverage]
public sealed class Int32NumericInputKind : NumericInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int32NumericInputKind"/> class.</summary>
    public Int32NumericInputKind()
        : base("Int32")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is int;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal((int)value);
}
