using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>True or false.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Flag")]
[ExcludeFromCodeCoverage]
public sealed class FlagCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FlagCacheKeyValueKind"/> class.</summary>
    public FlagCacheKeyValueKind()
        : base("Flag")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is bool;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => text.Append((bool)value ? "b1" : "b0");
}
