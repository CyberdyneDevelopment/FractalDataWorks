using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>Text, as it is.</summary>
[TypeOption(typeof(FilterTextKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class StringFilterTextKind : FilterTextKindBase
{
    /// <summary>Initializes a new instance of the <see cref="StringFilterTextKind"/> class.</summary>
    public StringFilterTextKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string;

    /// <inheritdoc />
    public override string Text(object value) => (string)value;
}
