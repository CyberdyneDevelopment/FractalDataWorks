using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A double, in the round-trip form.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Double")]
[ExcludeFromCodeCoverage]
public sealed class DoubleCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DoubleCacheKeyValueKind"/> class.</summary>
    public DoubleCacheKeyValueKind()
        : base("Double")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is double;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "r8", ((double)value).ToString("R", CacheKeyText.Invariant));
}
