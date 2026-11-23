using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A whole number of any width, a decimal or a GUID, written exactly under its type name.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Exact")]
[ExcludeFromCodeCoverage]
public sealed class ExactCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ExactCacheKeyValueKind"/> class.</summary>
    public ExactCacheKeyValueKind()
        : base("Exact")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or Int128 or UInt128 or decimal or Guid;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value) => CacheKeyText.Text(text, value.GetType().Name, ((IFormattable)value).ToString(null, CacheKeyText.Invariant));
}
