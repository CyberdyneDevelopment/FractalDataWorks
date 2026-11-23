using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A JSON true or false.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "JsonBoolean")]
public sealed class JsonBooleanEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonBooleanEventValueShape"/> class.</summary>
    public JsonBooleanEventValueShape()
        : base("JsonBoolean")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is JsonElement { ValueKind: JsonValueKind.True or JsonValueKind.False };

    /// <inheritdoc />
    public override bool? Flag(object value) => ((JsonElement)value).ValueKind == JsonValueKind.True;
}
