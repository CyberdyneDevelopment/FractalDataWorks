using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A number with a fraction, written as it is.</summary>
[TypeOption(typeof(ODataLiteralKinds), "Fractional")]
[ExcludeFromCodeCoverage]
public sealed class FractionalODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FractionalODataLiteralKind"/> class.</summary>
    public FractionalODataLiteralKind()
        : base("Fractional")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is decimal or double or float;

    /// <inheritdoc />
    public override string Literal(object value) => Invariant(value);
}
