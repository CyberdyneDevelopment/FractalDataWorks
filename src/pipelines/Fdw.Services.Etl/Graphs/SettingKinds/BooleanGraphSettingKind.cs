using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A setting that holds true or false.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphSettingKinds), "Boolean")]
public sealed class BooleanGraphSettingKind : GraphSettingKindBase
{
    /// <summary>Initializes a new instance of the <see cref="BooleanGraphSettingKind"/> class.</summary>
    public BooleanGraphSettingKind()
        : base("Boolean", "Bool", "true or false")
    {
    }

    /// <inheritdoc />
    public override object? ReadJson(JsonElement value) => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    /// <inheritdoc />
    public override bool TryConvert(object value, out object? typed)
    {
        typed = value is bool flag ? flag : null;
        return typed is not null;
    }
}
