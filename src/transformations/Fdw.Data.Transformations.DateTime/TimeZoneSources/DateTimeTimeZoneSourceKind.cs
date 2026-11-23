using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A date and time: one of unspecified kind is read as UTC.</summary>
[TypeOption(typeof(TimeZoneSourceKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeTimeZoneSourceKind : TimeZoneSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeTimeZoneSourceKind"/> class.</summary>
    public DateTimeTimeZoneSourceKind()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override DateTimeOffset InZone(object value, TimeZoneInfo timeZone)
    {
        var input = (DateTime)value;
        var utc = input.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(input, DateTimeKind.Utc)
            : input.ToUniversalTime();
        var converted = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        return new DateTimeOffset(converted, timeZone.GetUtcOffset(converted));
    }
}
