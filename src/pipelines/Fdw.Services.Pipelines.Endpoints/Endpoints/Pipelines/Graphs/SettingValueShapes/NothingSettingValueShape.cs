using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>No value: the JSON element that is none.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingValueShapes), "Nothing")]
public sealed class NothingSettingValueShape : SettingValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="NothingSettingValueShape"/> class.</summary>
    public NothingSettingValueShape()
        : base("Nothing")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object? value) => value is null;

    /// <inheritdoc />
    public override JsonElement ToElement(object? value, JsonSerializerOptions options) => default;
}
