using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>Text.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class StringCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="StringCacheKeyValueKind"/> class.</summary>
    public StringCacheKeyValueKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "s", (string)value);
}
