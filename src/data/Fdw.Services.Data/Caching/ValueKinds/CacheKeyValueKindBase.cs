using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;

namespace Fdw.Services.Data.Caching;

/// <summary>The base of every option of <see cref="CacheKeyValueKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class CacheKeyValueKindBase : TypeOptionBase<CacheKeyValueKindBase>, ICacheKeyValueKind
{
    /// <summary>Initializes a new instance of the <see cref="CacheKeyValueKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected CacheKeyValueKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract void Write(StringBuilder text, object value);
}
