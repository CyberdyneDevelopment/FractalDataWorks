using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A date, in the round-trip form.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Date")]
[ExcludeFromCodeCoverage]
public sealed class DateOnlyCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateOnlyCacheKeyValueKind"/> class.</summary>
    public DateOnlyCacheKeyValueKind()
        : base("Date")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateOnly;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, "date", ((DateOnly)value).ToString("O", CacheKeyText.Invariant));
}
