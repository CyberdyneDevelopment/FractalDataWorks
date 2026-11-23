using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A double or a float.</summary>
[TypeOption(typeof(FilterNumberKinds), "Fractional")]
[ExcludeFromCodeCoverage]
public sealed class FractionalFilterNumberKind : FilterNumberKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FractionalFilterNumberKind"/> class.</summary>
    public FractionalFilterNumberKind()
        : base("Fractional")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is double or float;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
}
