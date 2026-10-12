using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Finds data sets, data stores and pipelines by name from their domain tables, without composing any of them or building a graph.</summary>
/// <remarks>
/// <para>
/// A name lives on the domain row, so a search needs the domain table and nothing under it: one filtered, paged read of
/// that table per kind, answered by the store. Reading the whole scope into a graph to find a name (what the search used
/// to do on every call) composed every data set with all its child collections and then looked at the names.
/// </para>
/// <para>
/// A scope that is a list of attachments (a workspace) is searched by asking for exactly those ids, one read per hundred.
/// Rows are read as the caller, so row-level security narrows what is found, as it does for any list. A name is matched
/// again in memory with the case-insensitive comparison the search has always used, so what the store's collation
/// matches or misses never changes which rows are returned.
/// </para>
/// </remarks>
internal static class LineageSearchReader
{
    // A page of the filtered domain read, and how many ids one read of a workspace's attachments carries.
    private const int PageSize = 200;
    private const int IdsPerRead = 100;

    // A domain row whose implementation is the domain's own name is a row with no implementation behind it: not a member.
    private const string DataStoreDomain = "DataStore";

    // The kind a workspace's step in a path carries: a workspace is a scope, not a node the map draws.
    private const string WorkspaceKind = "Workspace";

    /// <summary>Finds the objects of the kinds asked for whose names match, the closest names first, one page of them.</summary>
    /// <param name="selection">The scope: everything the caller can see, or one workspace.</param>
    /// <param name="sources">The domain providers.</param>
    /// <param name="query">What to find and which page.</param>
    /// <param name="logger">Records a provider that cannot list its domain table.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The page, or the failure of the read that could not be answered.</returns>
    public static async Task<IGenericResult<LineageSearchPage>> Search(
        MapSelection selection, MapSources sources, LineageSearchQuery query, ILogger logger, CancellationToken ct)
    {
        var scope = selection.Describe();
        var matches = new List<LineageNode>();
        foreach (var kind in KindsOf(query.Kinds))
        {
            var rows = await ReadKind(kind, selection, sources, query, logger, ct).ConfigureAwait(false);
            if (rows.IsFailure) return rows.ToNewResult<LineageSearchPage>();

            matches.AddRange(rows.Value!.Select(row => new LineageNode(NodeId(kind, row.Id), kind, row.Name, scope.Workspace)));
        }

        var ordered = matches
            .OrderBy(node => LineageGraph.Closeness(node.Name, query.Text))
            .ThenBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(node => node.Kind, StringComparer.Ordinal)
            .ToList();
        var page = ordered.Skip(query.Skip).Take(query.Take).ToList();
        return GenericResult<LineageSearchPage>.Success(
            new LineageSearchPage(page, query.Skip, query.Take, ordered.Count > query.Skip + query.Take, Root(selection, scope)));
    }

    private static IEnumerable<string> KindsOf(LineageKindSelection kinds)
    {
        if (kinds.DataSets) yield return MapKinds.DataSet;
        if (kinds.DataStores) yield return MapKinds.DataStore;
        if (kinds.Pipelines) yield return MapKinds.Pipeline;
    }

    private static Task<IGenericResult<List<DomainConfiguration>>> ReadKind(
        string kind, MapSelection selection, MapSources sources, LineageSearchQuery query, ILogger logger, CancellationToken ct)
    {
        if (string.Equals(kind, MapKinds.DataStore, StringComparison.Ordinal))
            return Read(sources.DataStores, selection.Held(kind), query, ct);

        var records = string.Equals(kind, MapKinds.DataSet, StringComparison.Ordinal)
            ? sources.DataSetRecords(logger)
            : sources.PipelineRecords(logger);
        return records.IsFailure
            ? Task.FromResult(records.ToNewResult<List<DomainConfiguration>>())
            : Read(records.Value!, selection.Held(kind), query, ct);
    }

    private static async Task<IGenericResult<List<DomainConfiguration>>> Read<TDomain>(
        IDomainRecordProvider<TDomain> records, IReadOnlyCollection<Guid>? held, LineageSearchQuery query, CancellationToken ct)
        where TDomain : DomainConfiguration
    {
        var rows = new List<DomainConfiguration>();
        var read = held is null
            ? await ReadNamed(records, query, rows, ct).ConfigureAwait(false)
            : await ReadHeld(records, held, rows, ct).ConfigureAwait(false);
        if (read.IsFailure) return read.ToNewResult<List<DomainConfiguration>>();

        return GenericResult<List<DomainConfiguration>>.Success(rows.Where(row => Matches(row, query)).ToList());
    }

    // The store filters on the name and pages the rows; every page is read, since the closest names come first only after all matches are known.
    private static async Task<IGenericResult> ReadNamed<TDomain>(
        IDomainRecordProvider<TDomain> records, LineageSearchQuery query, List<DomainConfiguration> rows, CancellationToken ct)
        where TDomain : DomainConfiguration
    {
        var skip = 0;
        while (true)
        {
            var page = await records.Get(Named(query, skip), ct).ConfigureAwait(false);
            if (page.IsFailure) return page;

            await foreach (var row in page.Value!.Items.WithCancellation(ct).ConfigureAwait(false))
                rows.Add(row);

            if (page.Value.Outcome.IsFailure) return page.Value.Outcome;
            if (!page.Value.HasMore) return GenericResult.Success();

            skip += PageSize;
        }
    }

    private static async Task<IGenericResult> ReadHeld<TDomain>(
        IDomainRecordProvider<TDomain> records, IReadOnlyCollection<Guid> held, List<DomainConfiguration> rows, CancellationToken ct)
        where TDomain : DomainConfiguration
    {
        foreach (var ids in held.Chunk(IdsPerRead))
        {
            var read = await records.Get(ids, ct).ConfigureAwait(false);
            if (read.IsFailure) return read;

            rows.AddRange(read.Value!);
        }

        return GenericResult.Success();
    }

    private static DomainQuery Named(LineageSearchQuery query, int skip) => new(
        new FilterExpression
        {
            Root = new FilterCondition
            {
                PropertyName = nameof(DomainConfiguration.Name),
                Operator = FilterOperators.ByName(query.StartsWith ? "StartsWith" : "Contains"),
                Value = query.Text,
            },
        },
        null,
        skip,
        PageSize,
        false);

    private static bool Matches(DomainConfiguration row, LineageSearchQuery query)
    {
        if (string.IsNullOrWhiteSpace(row.Name) || string.IsNullOrWhiteSpace(row.Implementation)) return false;
        if (string.Equals(row.Implementation, DataStoreDomain, StringComparison.OrdinalIgnoreCase)) return false;

        return query.StartsWith
            ? row.Name.StartsWith(query.Text, StringComparison.OrdinalIgnoreCase)
            : row.Name.Contains(query.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static string NodeId(string kind, Guid id) => kind switch
    {
        MapKinds.DataSet => MapNodeIds.DataSet(id),
        MapKinds.DataStore => MapNodeIds.DataStore(id),
        _ => MapNodeIds.Pipeline(id),
    };

    // Only a workspace can root a path: the scope of a search over everything is not an object.
    private static LineagePathStepDto? Root(MapSelection selection, MapScopeDto scope)
        => scope.Workspace is null
            ? null
            : new LineagePathStepDto
            {
                Id = selection.ScopeId is { } id ? MapNodeIds.Other(WorkspaceKind, id) : null,
                Kind = WorkspaceKind,
                Name = scope.Workspace,
            };
}
