using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A decimal.</summary>
[TypeOption(typeof(FilterNumberKinds), "Decimal")]
[ExcludeFromCodeCoverage]
public sealed class DecimalFilterNumberKind : FilterNumberKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalFilterNumberKind"/> class.</summary>
    public DecimalFilterNumberKind()
        : base("Decimal")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is decimal;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => (decimal)value;
}
