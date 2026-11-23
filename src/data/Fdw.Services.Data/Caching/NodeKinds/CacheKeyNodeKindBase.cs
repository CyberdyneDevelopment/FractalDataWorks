using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>The base of every option of <see cref="CacheKeyNodeKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class CacheKeyNodeKindBase : TypeOptionBase<CacheKeyNodeKindBase>, ICacheKeyNodeKind
{
    /// <summary>Initializes a new instance of the <see cref="CacheKeyNodeKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected CacheKeyNodeKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(IFilterNode node);

    /// <inheritdoc />
    public abstract void Write(StringBuilder text, IFilterNode node);
}
