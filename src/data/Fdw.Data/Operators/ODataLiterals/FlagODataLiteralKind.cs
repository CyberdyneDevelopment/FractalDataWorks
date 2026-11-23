using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>True or false, in lowercase.</summary>
[TypeOption(typeof(ODataLiteralKinds), "Flag")]
[ExcludeFromCodeCoverage]
public sealed class FlagODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FlagODataLiteralKind"/> class.</summary>
    public FlagODataLiteralKind()
        : base("Flag")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is bool;

    /// <inheritdoc />
    public override string Literal(object value) => (bool)value ? "true" : "false";
}
