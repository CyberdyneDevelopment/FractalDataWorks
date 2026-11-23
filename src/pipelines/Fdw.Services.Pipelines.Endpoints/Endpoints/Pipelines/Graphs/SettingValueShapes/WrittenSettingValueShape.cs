using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Any other object, written as JSON; no value matches it, it is what <c>Of</c> answers when no other shape does.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SettingValueShapes), "Written")]
public sealed class WrittenSettingValueShape : SettingValueShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="WrittenSettingValueShape"/> class.</summary>
    public WrittenSettingValueShape()
        : base("Written")
    {
    }

    /// <inheritdoc />
    public override bool Matches(object? value) => false;

    /// <inheritdoc />
    public override JsonElement ToElement(object? value, JsonSerializerOptions options) => JsonSerializer.SerializeToElement(value, options);
}
