using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>The aggregate functions a store can compute: Sum, Count, Average, Minimum and Maximum.</summary>
/// <remarks>A word no function answers to gives the NotFound sentinel; the caller refuses the request.</remarks>
[TypeCollection(typeof(SqlAggregateBase), typeof(ISqlAggregate), typeof(SqlAggregates))]
[ExcludeFromCodeCoverage]
public abstract partial class SqlAggregates : TypeCollectionBase<SqlAggregateBase, ISqlAggregate>
{
    /// <summary>Finds the function a data query request names.</summary>
    /// <param name="name">The name, in any case.</param>
    /// <returns>The function, or <c>NotFound</c>.</returns>
    public static ISqlAggregate OfQueryName(string? name)
    {
        foreach (var aggregate in All())
        {
            if (string.Equals(aggregate.QueryName, name, StringComparison.OrdinalIgnoreCase))
            {
                return aggregate;
            }
        }

        return NotFound;
    }

    /// <summary>Finds the function a data set's aggregate definition names.</summary>
    /// <param name="word">The word, in any case, with or without surrounding white space.</param>
    /// <returns>The function, or <c>NotFound</c>.</returns>
    public static ISqlAggregate OfAlias(string? word)
    {
        foreach (var aggregate in All())
        {
            if (aggregate.Aliases.Any(alias => string.Equals(alias, word?.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return aggregate;
            }
        }

        return NotFound;
    }
}
