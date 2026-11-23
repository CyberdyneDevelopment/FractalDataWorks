using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>Text that reads as a date and time.</summary>
[TypeOption(typeof(TimeZoneSourceKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextTimeZoneSourceKind : TimeZoneSourceKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextTimeZoneSourceKind"/> class.</summary>
    public TextTimeZoneSourceKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string text && DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    /// <inheritdoc />
    public override DateTimeOffset InZone(object value, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.Parse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.None), timeZone);
}
