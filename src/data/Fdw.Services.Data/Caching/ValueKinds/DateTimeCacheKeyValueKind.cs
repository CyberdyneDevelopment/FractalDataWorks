using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A date and time, in the round-trip form.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeCacheKeyValueKind"/> class.</summary>
    public DateTimeCacheKeyValueKind()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "dt", ((DateTime)value).ToString("O", CacheKeyText.Invariant));
}
