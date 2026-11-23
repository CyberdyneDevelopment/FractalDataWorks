using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A float, in the round-trip form.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Float")]
[ExcludeFromCodeCoverage]
public sealed class FloatCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatCacheKeyValueKind"/> class.</summary>
    public FloatCacheKeyValueKind()
        : base("Float")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is float;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "r4", ((float)value).ToString("R", CacheKeyText.Invariant));
}
