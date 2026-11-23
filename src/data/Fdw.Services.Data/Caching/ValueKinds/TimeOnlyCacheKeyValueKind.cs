using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A time of day, in the round-trip form.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Time")]
[ExcludeFromCodeCoverage]
public sealed class TimeOnlyCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeOnlyCacheKeyValueKind"/> class.</summary>
    public TimeOnlyCacheKeyValueKind()
        : base("Time")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is TimeOnly;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "time", ((TimeOnly)value).ToString("O", CacheKeyText.Invariant));
}
