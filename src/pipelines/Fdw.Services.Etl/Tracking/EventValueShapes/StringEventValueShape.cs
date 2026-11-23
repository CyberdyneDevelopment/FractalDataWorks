using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A string: read as a number or a time when it is written as one, and as a text as it is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "String")]
public sealed class StringEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="StringEventValueShape"/> class.</summary>
    public StringEventValueShape()
        : base("String")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is string;

    /// <inheritdoc />
    public override long? Number(object value) => long.TryParse((string)value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;

    /// <inheritdoc />
    public override DateTimeOffset? Time(object value) => DateTimeOffset.TryParse((string)value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ? time : null;

    /// <inheritdoc />
    public override string? Text(object value) => (string)value;
}
