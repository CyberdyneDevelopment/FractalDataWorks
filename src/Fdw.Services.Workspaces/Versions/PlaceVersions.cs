using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The versions of a definition held at places, read for a caller (FDW-2331, FDW-2560): the versions at the places the caller may read, nearest the caller
/// first with the one that applies marked, and how many of them each definition on a page has. A kind of definition states its
/// <see cref="VersionSource{TRow}"/> and what a version says; the places, the rights and the reads are the same for every kind.
/// </summary>
public static class PlaceVersions
{
    /// <summary>The most versions of one definition that are read; a definition has at most one per place.</summary>
    public const int MaxVersions = 1000;

    /// <summary>Lists the versions of one definition at the places the caller may read.</summary>
    /// <typeparam name="TRow">The domain row of a version.</typeparam>
    /// <typeparam name="TPayload">What a version says.</typeparam>
    /// <param name="source">Where the versions are read from.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="names">Says the name of an organization or a workspace.</param>
    /// <param name="baseId">The id of the definition.</param>
    /// <param name="definitionName">The name of the definition, which every entry is labelled with.</param>
    /// <param name="basePayload">What the definition itself says, which is the tenant's version.</param>
    /// <param name="payloadOf">Reads what a version row says.</param>
    /// <param name="origin">The place the caller reads at.</param>
    /// <param name="readRight">The right that lets the caller read a version at a place.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The versions, nearest the caller first; a place the caller may not read, and a place that holds no version, are absent. Or the failure when the
    /// place is not in the caller's tenant or a read failed.
    /// </returns>
    public static async Task<IGenericResult<IReadOnlyList<PlaceVersion<TPayload>>>> Entries<TRow, TPayload>(
        VersionSource<TRow> source,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IPlaceNames names,
        Guid baseId,
        string definitionName,
        TPayload basePayload,
        Func<TRow, TPayload> payloadOf,
        CallerPlace origin,
        string readRight,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TRow : class, IDomainConfiguration
    {
        var steps = await CallerPlaces.Above(ancestry, origin, logger, cancellationToken).ConfigureAwait(false);
        if (steps.IsFailure || steps.Value is not { } places)
        {
            return steps.ToNewResult<IReadOnlyList<PlaceVersion<TPayload>>>();
        }

        var held = await Held(source, new FilterExpression { Root = Node(source.BaseIdProperty, "Equal", baseId) }, MaxVersions, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure || held.Value is not { } rows)
        {
            return held.ToNewResult<IReadOnlyList<PlaceVersion<TPayload>>>();
        }

        var entries = new List<PlaceVersion<TPayload>>();
        foreach (var step in places)
        {
            var isBase = string.Equals(step.Kind, PlaceKinds.Tenant, StringComparison.Ordinal);
            TPayload payload;
            if (isBase)
            {
                payload = basePayload;
            }
            else if (rows.FirstOrDefault(row => source.ScopeId(row) == step.Id && string.Equals(source.ScopeKind(row), step.Kind, StringComparison.Ordinal)) is { } found)
            {
                payload = payloadOf(found);
            }
            else
            {
                continue;
            }

            if (CallerPlaces.ScopeOf(places, step) is not { } scope
                || (await authority.May(readRight, scope, cancellationToken).ConfigureAwait(false)).IsFailure)
            {
                continue;
            }

            var label = await LabelOf(names, definitionName, step, isBase, cancellationToken).ConfigureAwait(false);
            if (label.IsFailure || label.Value is not { } shown)
            {
                return label.ToNewResult<IReadOnlyList<PlaceVersion<TPayload>>>();
            }

            entries.Add(new PlaceVersion<TPayload>(step.Kind, step.Id, step.Depth, payload, shown, isBase, entries.Count == 0));
        }

        return GenericResult<IReadOnlyList<PlaceVersion<TPayload>>>.Success(entries);
    }

    /// <summary>
    /// Counts the versions of each definition on a page that the caller may read, the definition itself counting as one. The versions of every definition on
    /// the page are read in one query, narrowed to those definitions and to the places the caller may read, so the cost of a page does not grow with it.
    /// </summary>
    /// <typeparam name="TRow">The domain row of a version.</typeparam>
    /// <param name="source">Where the versions are read from.</param>
    /// <param name="ancestry">Says which places are at or above a place.</param>
    /// <param name="authority">Says whether the caller holds a right at a place.</param>
    /// <param name="baseIds">The ids of the definitions on the page.</param>
    /// <param name="origin">The place the caller reads at.</param>
    /// <param name="readRight">The right that lets the caller read a version at a place.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The count of each definition, or the failure when the place is not in the caller's tenant or a read failed.</returns>
    public static async Task<IGenericResult<IReadOnlyDictionary<Guid, int>>> Counts<TRow>(
        VersionSource<TRow> source,
        IPlaceAncestry ancestry,
        IScopeAuthority authority,
        IReadOnlyList<Guid> baseIds,
        CallerPlace origin,
        string readRight,
        ILogger logger,
        CancellationToken cancellationToken = default)
        where TRow : class, IDomainConfiguration
    {
        var counts = baseIds.ToDictionary(id => id, _ => 1);
        if (baseIds.Count == 0)
        {
            return GenericResult<IReadOnlyDictionary<Guid, int>>.Success(counts);
        }

        var steps = await CallerPlaces.Above(ancestry, origin, logger, cancellationToken).ConfigureAwait(false);
        if (steps.IsFailure || steps.Value is not { } places)
        {
            return steps.ToNewResult<IReadOnlyDictionary<Guid, int>>();
        }

        // The tenant's version is the definition itself, which every count already holds; a version is held at an organization or a workspace.
        var readable = new HashSet<(string Kind, Guid Id)>();
        foreach (var step in places.Where(step => !string.Equals(step.Kind, PlaceKinds.Tenant, StringComparison.Ordinal)))
        {
            if (CallerPlaces.ScopeOf(places, step) is { } scope
                && (await authority.May(readRight, scope, cancellationToken).ConfigureAwait(false)).IsSuccess)
            {
                readable.Add((step.Kind, step.Id));
            }
        }

        if (readable.Count == 0)
        {
            return GenericResult<IReadOnlyDictionary<Guid, int>>.Success(counts);
        }

        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.And,
                Nodes =
                [
                    Node(source.BaseIdProperty, "In", baseIds.Cast<object?>().ToList()),
                    Node(source.ScopeIdProperty, "In", readable.Select(place => (object?)place.Id).ToList()),
                ],
            },
        };
        var held = await Held(source, filter, baseIds.Count * readable.Count, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure || held.Value is not { } rows)
        {
            return held.ToNewResult<IReadOnlyDictionary<Guid, int>>();
        }

        foreach (var row in rows.Where(row => readable.Contains((source.ScopeKind(row), source.ScopeId(row)))))
        {
            counts[source.BaseId(row)] += 1;
        }

        return GenericResult<IReadOnlyDictionary<Guid, int>>.Success(counts);
    }

    // What to show for a version: the definition's name, and for an organization's or a workspace's version the place's name after it in parentheses.
    private static async Task<IGenericResult<string>> LabelOf(
        IPlaceNames names, string definitionName, PlaceStep step, bool isBase, CancellationToken cancellationToken)
    {
        if (isBase)
        {
            return GenericResult<string>.Success(definitionName);
        }

        var scopeName = await names.Get(step.Kind, step.Id, cancellationToken).ConfigureAwait(false);
        return scopeName.IsFailure || scopeName.Value is not { } named
            ? scopeName
            : GenericResult<string>.Success($"{definitionName} ({named})");
    }

    /// <summary>Reads the version rows a filter keeps, at most take of them.</summary>
    /// <typeparam name="TRow">The domain row of a version.</typeparam>
    /// <param name="source">Where the versions are read from.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="take">The most rows to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows, or the failure of the read.</returns>
    public static async Task<IGenericResult<IReadOnlyList<TRow>>> Held<TRow>(
        VersionSource<TRow> source, IFilterExpression filter, int take, CancellationToken cancellationToken = default)
        where TRow : class, IDomainConfiguration
    {
        var page = await source.Provider.Get(new DomainQuery(filter, null, 0, take, false), cancellationToken).ConfigureAwait(false);
        if (page.IsFailure || page.Value is not { } read)
        {
            return page.ToNewResult<IReadOnlyList<TRow>>();
        }

        var rows = new List<TRow>();
        await foreach (var row in read.Items.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(row);
        }

        return read.Outcome.IsFailure
            ? read.Outcome.ToNewResult<IReadOnlyList<TRow>>()
            : GenericResult<IReadOnlyList<TRow>>.Success(rows);
    }

    private static FilterCondition Node(string property, string operatorName, object value) =>
        new() { PropertyName = property, Operator = FilterOperators.ByName(operatorName), Value = value };
}
