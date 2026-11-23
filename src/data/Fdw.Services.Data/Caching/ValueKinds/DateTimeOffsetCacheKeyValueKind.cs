using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A date and time with an offset, in the round-trip form.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetCacheKeyValueKind"/> class.</summary>
    public DateTimeOffsetCacheKeyValueKind()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "dto", ((DateTimeOffset)value).ToString("O", CacheKeyText.Invariant));
}
