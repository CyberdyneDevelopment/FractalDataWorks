using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A value read from a request body: its raw text is exact.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Json")]
[ExcludeFromCodeCoverage]
public sealed class JsonCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonCacheKeyValueKind"/> class.</summary>
    public JsonCacheKeyValueKind()
        : base("Json")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is System.Text.Json.JsonElement;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value)
    {
        var json = (System.Text.Json.JsonElement)value;
        CacheKeyText.Text(text, "json", json.ValueKind.ToString());
        CacheKeyText.Text(text, "v", json.GetRawText());
    }
}
