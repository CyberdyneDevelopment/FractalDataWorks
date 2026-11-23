using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A JSON string.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "JsonString")]
public sealed class JsonStringEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonStringEventValueShape"/> class.</summary>
    public JsonStringEventValueShape()
        : base("JsonString")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is JsonElement { ValueKind: JsonValueKind.String };

    /// <inheritdoc />
    public override DateTimeOffset? Time(object value) => DateTimeOffset.TryParse(((JsonElement)value).GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time) ? time : null;

    /// <inheritdoc />
    public override string? Text(object value) => ((JsonElement)value).GetString();
}
