using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A byte.</summary>
[TypeOption(typeof(NumericInputKinds), "Byte")]
[ExcludeFromCodeCoverage]
public sealed class ByteNumericInputKind : NumericInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ByteNumericInputKind"/> class.</summary>
    public ByteNumericInputKind()
        : base("Byte")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is byte;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal((byte)value);
}
