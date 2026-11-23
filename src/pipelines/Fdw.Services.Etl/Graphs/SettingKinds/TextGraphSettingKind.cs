using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting that holds text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphSettingKinds), "Text")]
public sealed class TextGraphSettingKind : GraphSettingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextGraphSettingKind"/> class.</summary>
    public TextGraphSettingKind()
        : base("Text", "String", "text")
    {
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <inheritdoc />
    public override bool TryConvert(object value, out object? typed)
    {
        typed = global::System.Convert.ToString(value, CultureInfo.InvariantCulture);
        return typed is not null;
    }
}
