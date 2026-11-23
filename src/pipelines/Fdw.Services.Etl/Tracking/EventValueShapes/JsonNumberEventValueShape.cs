using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Tracking;

/// <summary>A JSON number.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EventValueShapes), "JsonNumber")]
public sealed class JsonNumberEventValueShape : EventValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonNumberEventValueShape"/> class.</summary>
    public JsonNumberEventValueShape()
        : base("JsonNumber")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object value) => value is JsonElement { ValueKind: JsonValueKind.Number };

    /// <inheritdoc />
    public override long? Number(object value) => ((JsonElement)value).TryGetInt64(out var number) ? number : null;
}
