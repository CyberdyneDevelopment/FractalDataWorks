using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A whole number of any width.</summary>
[TypeOption(typeof(FilterNumberKinds), "Whole")]
[ExcludeFromCodeCoverage]
public sealed class WholeFilterNumberKind : FilterNumberKindBase
{
    /// <summary>Initializes a new instance of the <see cref="WholeFilterNumberKind"/> class.</summary>
    public WholeFilterNumberKind()
        : base("Whole")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is int or long or short or byte or sbyte or uint or ulong or ushort;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
}
