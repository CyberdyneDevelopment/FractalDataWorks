using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>Text: quoted, with a quote inside it doubled.</summary>
[TypeOption(typeof(ODataLiteralKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextODataLiteralKind"/> class.</summary>
    public TextODataLiteralKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string;

    /// <inheritdoc />
    public override string Literal(object value) => $"'{((string)value).Replace("'", "''", StringComparison.Ordinal)}'";
}
