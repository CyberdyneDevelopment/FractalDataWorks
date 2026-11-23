using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data;

/// <summary>A date and time with an offset: datetimeoffset'...'.</summary>
[TypeOption(typeof(ODataLiteralKinds), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetODataLiteralKind : ODataLiteralKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetODataLiteralKind"/> class.</summary>
    public DateTimeOffsetODataLiteralKind()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override string Literal(object value) => $"datetimeoffset'{((DateTimeOffset)value).ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture)}'";
}
