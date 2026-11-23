using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Fdw.Commands.Data.Abstractions;
using Fdw.Commands.Data.Abstractions.Caching;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>
/// Writes the canonical text of a query: every member that can change what the query answers. Each string is written with its
/// length in front, so no delimiter can be forged, and each value with its type and an exact form, so two queries share a text
/// only when they ask the same thing (FDW-3620). <see cref="CacheKeyBuilder"/> keys a cached read on a digest of it.
/// </summary>
/// <remarks>
/// Why text and not <see cref="object.GetHashCode"/>: a 32-bit hash lets two different filters share a value, and so a cache
/// entry, and it is only as good as each expression type's override (a projection or aggregation with none hashes by reference).
/// A value or a filter node this cannot write exactly is refused, never keyed on a guess.
/// </remarks>
internal static class CacheKeyText
{
    internal static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    // The Metadata keys that steer the cache rather than the read: they never change what a query answers. The command's
    // Metadata is case-insensitive, so the match is too.
    private static readonly HashSet<string> CachePolicyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        CachePolicy.CacheEnabledKey,
        CachePolicy.CacheDurationKey,
        CachePolicy.CacheKeyPrefixKey,
        CachePolicy.CacheInvalidationTagsKey,
    };

    /// <summary>
    /// Writes the canonical text of <paramref name="query"/>.
    /// </summary>
    /// <param name="query">The query to describe.</param>
    /// <returns>The text: equal for two queries exactly when every member that changes their answer is equal.</returns>
    /// <exception cref="InvalidOperationException">A filter node, filter value or Metadata value has no exact text form.</exception>
    public static string For(IQueryCommand query)
    {
        var text = new StringBuilder(256);
        Text(text, "type", query.CommandType);
        Text(text, "category", query.Category);
        Filter(text, query.Filter);
        Ordering(text, query.Ordering);
        Paging(text, query.Paging);
        Projection(text, query.Projection);
        Aggregation(text, query.Aggregation);
        Joins(text, query.Joins);
        Unions(text, query.Unions);
        Flags(text, query);
        Metadata(text, query.Metadata);
        return text.ToString();
    }

    private static void Filter(StringBuilder text, IFilterExpression? filter)
    {
        text.Append("filter=");
        if (filter?.Root is { } root)
            Node(text, root);
        else
            text.Append('-');
    }

    private static void Ordering(StringBuilder text, IOrderingExpression? ordering)
    {
        text.Append(";ordering=");
        if (ordering is null)
        {
            text.Append('-');
            return;
        }

        foreach (var field in ordering.OrderedFields)
        {
            Text(text, "p", field.PropertyName);
            Text(text, "d", field.Direction.Name);
        }
    }

    private static void Paging(StringBuilder text, IPagingExpression? paging)
    {
        text.Append(";paging=");
        if (paging is null)
        {
            text.Append('-');
            return;
        }

        text.Append(paging.Skip.ToString(Invariant)).Append(',').Append(paging.Take is { } take ? take.ToString(Invariant) : "-");
    }

    private static void Projection(StringBuilder text, IProjectionExpression? projection)
    {
        text.Append(";projection=");
        if (projection is null)
        {
            text.Append('-');
            return;
        }

        foreach (var field in projection.Fields)
        {
            Text(text, "p", field.PropertyName);
            NullableText(text, "a", field.Alias);
            NullableText(text, "s", field.SourceContainer);
        }

        text.Append("|names=");
        if (projection.PropertyNames is { } names)
        {
            foreach (var name in names)
                Text(text, "n", name);
        }
        else
        {
            text.Append('-');
        }
    }

    private static void Aggregation(StringBuilder text, IAggregationExpression? aggregation)
    {
        text.Append(";aggregation=");
        if (aggregation is null)
        {
            text.Append('-');
            return;
        }

        foreach (var field in aggregation.GroupByFields)
            Text(text, "g", field);

        // In the order given: the order of the aggregate columns is the order of the answer's columns.
        foreach (var (column, function) in aggregation.Aggregations)
        {
            Text(text, "c", column);
            Text(text, "f", function);
        }

        text.Append(aggregation.IncludeGroupCount ? "|count" : "|nocount");
    }

    private static void Joins(StringBuilder text, IReadOnlyList<IJoinExpression> joins)
    {
        text.Append(";joins=");
        foreach (var join in joins)
        {
            Text(text, "t", join.TargetContainerName);
            NullableText(text, "s", join.SourceContainerName);
            Text(text, "k", join.JoinType);
            foreach (var (left, right) in join.JoinConditions)
            {
                Text(text, "l", left);
                Text(text, "r", right);
            }
        }
    }

    // The containers a union stacks, each with its columns in order (FDW-6946): another branch, or the same columns in another order, is
    // another answer.
    private static void Unions(StringBuilder text, IReadOnlyList<IUnionBranch>? unions)
    {
        text.Append(";unions=");
        foreach (var branch in unions ?? [])
        {
            Text(text, "u", branch.ContainerName);
            foreach (var field in branch.Fields)
            {
                Text(text, "p", field.PropertyName);
                NullableText(text, "a", field.Alias);
            }
        }
    }

    private static void Flags(StringBuilder text, IQueryCommand query)
        => text.Append(";flags=")
            .Append(query.ReadsForWrite ? 'W' : 'w')
            .Append(query.IncludeMetadata ? 'M' : 'm')
            .Append(query.IncludeDescriptions ? 'D' : 'd')
            .Append(query.IncludeKeys ? 'K' : 'k');

    // A connection may read the shape of its request from Metadata (an Http GraphQL query or SOAP action), so every entry but
    // the cache's own is part of the question. Sorted, because the dictionary's order means nothing.
    private static void Metadata(StringBuilder text, IReadOnlyDictionary<string, object> metadata)
    {
        text.Append(";metadata=");
        foreach (var entry in metadata.Where(entry => !CachePolicyKeys.Contains(entry.Key)).OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            Text(text, "k", entry.Key);
            Value(text, entry.Value);
        }
    }

    private static void Node(StringBuilder text, IFilterNode node) => CacheKeyNodeKinds.Append(text, node);

    // A value carries its type, so 1 and "1" (or an int and a long 1) never share text, and an exact form: the round-trip
    // format where the default one drops precision (DateTime, TimeOnly, double).
    internal static void Value(StringBuilder text, object? value) => CacheKeyValueKinds.Append(text, value);

    internal static void Text(StringBuilder text, string tag, string value)
        => text.Append(tag).Append(value.Length.ToString(Invariant)).Append(':').Append(value);

    private static void NullableText(StringBuilder text, string tag, string? value)
    {
        if (value is null)
            text.Append(tag).Append('-');
        else
            Text(text, tag, value);
    }
}
