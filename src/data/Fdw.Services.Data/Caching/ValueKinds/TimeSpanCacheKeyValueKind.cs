using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A length of time, in the constant form.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Span")]
[ExcludeFromCodeCoverage]
public sealed class TimeSpanCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeSpanCacheKeyValueKind"/> class.</summary>
    public TimeSpanCacheKeyValueKind()
        : base("Span")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is TimeSpan;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "span", ((TimeSpan)value).ToString("c", CacheKeyText.Invariant));
}
