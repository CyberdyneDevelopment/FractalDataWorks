using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A value that is JSON already.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingValueShapes), "Json")]
public sealed class JsonSettingValueShape : SettingValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonSettingValueShape"/> class.</summary>
    public JsonSettingValueShape()
        : base("Json")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object? value) => value is JsonElement;

    /// <inheritdoc />
    public override JsonElement ToElement(object? value, JsonSerializerOptions options) => (JsonElement)value!;
}
