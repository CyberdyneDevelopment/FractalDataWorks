using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>Any other value, as the text it reports for itself.</summary>
[TypeOption(typeof(FilterTextKinds), "Plain")]
[ExcludeFromCodeCoverage]
public sealed class PlainFilterTextKind : FilterTextKindBase
{
    /// <summary>Initializes a new instance of the <see cref="PlainFilterTextKind"/> class.</summary>
    public PlainFilterTextKind()
        : base("Plain")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is not string and not IFormattable;

    /// <inheritdoc />
    public override string Text(object value) => value.ToString() ?? string.Empty;
}
