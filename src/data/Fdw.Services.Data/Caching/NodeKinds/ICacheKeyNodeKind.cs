using System;
using System.Text;
using Fdw.Collections;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>One kind of node a filter tree is made of, and how it is written into a cache key.</summary>
public interface ICacheKeyNodeKind : ITypeOption<int, CacheKeyNodeKindBase>
{
    /// <summary>Tells whether a node is of this kind.</summary>
    /// <param name="node">The node.</param>
    /// <returns>True when this kind holds the node.</returns>
    bool Holds(IFilterNode node);

    /// <summary>Writes a node of this kind into a cache key.</summary>
    /// <param name="text">The key being built.</param>
    /// <param name="node">A node this kind <see cref="Holds"/>.</param>
    void Write(StringBuilder text, IFilterNode node);
}
