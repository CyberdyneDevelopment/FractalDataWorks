using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A date and time: datetime'...'.</summary>
[TypeOption(typeof(ODataLiteralKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeODataLiteralKind"/> class.</summary>
    public DateTimeODataLiteralKind()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override string Literal(object value) => $"datetime'{((DateTime)value).ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)}'";
}
