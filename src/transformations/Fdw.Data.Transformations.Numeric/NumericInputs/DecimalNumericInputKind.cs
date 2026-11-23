using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A decimal, kept as it is.</summary>
[TypeOption(typeof(NumericInputKinds), "Decimal")]
[ExcludeFromCodeCoverage]
public sealed class DecimalNumericInputKind : NumericInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalNumericInputKind"/> class.</summary>
    public DecimalNumericInputKind()
        : base("Decimal")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is decimal;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => (decimal)value;
}
