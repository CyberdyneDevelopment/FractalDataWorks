using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>The kinds of filter node written into a cache key.</summary>
[TypeCollection(typeof(CacheKeyNodeKindBase), typeof(ICacheKeyNodeKind), typeof(CacheKeyNodeKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class CacheKeyNodeKinds : TypeCollectionBase<CacheKeyNodeKindBase, ICacheKeyNodeKind>
{
    /// <summary>Writes a filter node into a cache key by the kind it is.</summary>
    /// <param name="text">The key being built.</param>
    /// <param name="node">The node.</param>
    /// <exception cref="InvalidOperationException">The node is neither a condition nor a group, so a query holding it has no cache key.</exception>
    public static void Append(StringBuilder text, IFilterNode node)
    {
        foreach (var kind in All())
        {
            if (kind.Holds(node))
            {
                kind.Write(text, node);
                return;
            }
        }

        throw new InvalidOperationException(
            $"A filter node of type '{node.GetType().FullName}' is neither a condition nor a group, so a query holding it has no cache key.");
    }
}
