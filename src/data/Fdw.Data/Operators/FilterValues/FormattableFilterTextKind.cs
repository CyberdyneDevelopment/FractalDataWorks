using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A number, date or other value that formats itself, in the invariant culture.</summary>
[TypeOption(typeof(FilterTextKinds), "Formattable")]
[ExcludeFromCodeCoverage]
public sealed class FormattableFilterTextKind : FilterTextKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FormattableFilterTextKind"/> class.</summary>
    public FormattableFilterTextKind()
        : base("Formattable")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is IFormattable;

    /// <inheritdoc />
    public override string Text(object value) => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
}
