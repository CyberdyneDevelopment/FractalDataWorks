using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Caching;

/// <summary>A sequence of values, each written with its own type.</summary>
[TypeOption(typeof(CacheKeyValueKinds), "Sequence")]
[ExcludeFromCodeCoverage]
public sealed class SequenceCacheKeyValueKind : CacheKeyValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="SequenceCacheKeyValueKind"/> class.</summary>
    public SequenceCacheKeyValueKind()
        : base("Sequence")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is System.Collections.IEnumerable and not string;

    /// <inheritdoc />
    public override void Write(StringBuilder text, object value)
    {
        text.Append('[');
        foreach (var item in (System.Collections.IEnumerable)value)
            CacheKeyText.Value(text, item);
        text.Append(']');
    }
}
