using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A GUID: guid'...'.</summary>
[TypeOption(typeof(ODataLiteralKinds), "Guid")]
[ExcludeFromCodeCoverage]
public sealed class GuidODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="GuidODataLiteralKind"/> class.</summary>
    public GuidODataLiteralKind()
        : base("Guid")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is Guid;

    /// <inheritdoc />
    public override string Literal(object value) => $"guid'{((Guid)value).ToString("D", CultureInfo.InvariantCulture)}'";
}
