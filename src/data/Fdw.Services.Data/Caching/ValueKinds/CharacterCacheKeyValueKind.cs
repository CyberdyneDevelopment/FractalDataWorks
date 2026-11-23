using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A character.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Character")]
[ExcludeFromCodeCoverage]
public sealed class CharacterCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="CharacterCacheKeyValueKind"/> class.</summary>
    public CharacterCacheKeyValueKind()
        : base("Character")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is char;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "ch", ((char)value).ToString(CacheKeyText.Invariant));
}
