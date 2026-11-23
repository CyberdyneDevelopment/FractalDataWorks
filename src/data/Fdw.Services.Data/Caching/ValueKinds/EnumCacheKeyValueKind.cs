using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>An enum: its type and its number.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Enum")]
[ExcludeFromCodeCoverage]
public sealed class EnumCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="EnumCacheKeyValueKind"/> class.</summary>
    public EnumCacheKeyValueKind()
        : base("Enum")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is Enum;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value)
    {
        var member = (Enum)value;
        CacheKeyText.Text(text, "enum", member.GetType().FullName!);
        CacheKeyText.Text(text, "v", member.ToString("D"));
    }
}
