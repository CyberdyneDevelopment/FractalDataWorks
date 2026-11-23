using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A whole number, written as it is.</summary>
[TypeOption(typeof(ODataLiteralKinds), "Whole")]
[ExcludeFromCodeCoverage]
public sealed class WholeODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="WholeODataLiteralKind"/> class.</summary>
    public WholeODataLiteralKind()
        : base("Whole")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is int or long or short or byte;

    /// <inheritdoc />
    public override string Literal(object value) => Invariant(value);
}
