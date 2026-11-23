using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A date and time with an offset.</summary>
[TypeOption(typeof(TimeZoneSourceKinds), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetTimeZoneSourceKind : TimeZoneSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetTimeZoneSourceKind"/> class.</summary>
    public DateTimeOffsetTimeZoneSourceKind()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override DateTimeOffset InZone(object value, TimeZoneInfo timeZone) => TimeZoneInfo.ConvertTime((DateTimeOffset)value, timeZone);
}
