using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Counts what a map scope holds from the domain tables alone: one count each for the kinds the platform lists, nothing read or composed.</summary>
/// <remarks>
/// <para>
/// A scope that is a list of attachments (a workspace) is counted from that list. Everything is counted by one query per
/// kind that asks the store for the total of the domain table, so the cost does not grow with the estate. The counts are
/// of domain rows, so a data store the platform itself declares is counted though a list of stores leaves it out.
/// </para>
/// <para>
/// Connections, quality rules and schedules are the platform's own members, counted from their domain tables in the
/// everything scope only: a workspace scope holds none of them and an organization scope is chosen by reading pipelines.
/// </para>
/// <para>
/// What this does not count is named, not zeroed: kinds whose rows are children of another object (paths, containers,
/// fields, stages, branch paths, field transformations), workspaces (whether the caller may see one is decided per workspace
/// by the access policy, which a total of the table cannot honour), every relation (an edge is derived from reading the
/// objects), and any kind of an organization scope (which is chosen by reading pipelines).
/// </para>
/// </remarks>
internal static class MapSummaryReader
{
    // The kinds the map can hold, in the order a client lists them; each is counted here or named as not counted.
    private static readonly string[] CountedKinds = [MapKinds.DataSet, MapKinds.DataStore, MapKinds.Pipeline];

    private static readonly string[] OtherKinds =
    [
        "Workspace", "Path", "Container", "ContainerField", "DataSetField", "Stage", "BranchPath", "FieldTransformation",
    ];

    private static readonly string[] Relations =
    [
        MapRelations.ConsumedBy, MapRelations.Produces, MapRelations.StoredIn, MapRelations.PartOf, MapRelations.DerivedFrom, MapRelations.RelatesTo,
    ];

    /// <summary>Counts what the scope holds.</summary>
    /// <param name="selection">The scope.</param>
    /// <param name="sources">The domain providers.</param>
    /// <param name="counts">The domain providers for the kinds no selection holds.</param>
    /// <param name="logger">Records a provider that cannot list its domain table.</param>
    /// <param name="ct">The cancellation token.</param>
    public static async Task<IGenericResult<MapSummaryResponse>> Count(
        MapSelection selection, MapSources sources, MapKindCounts counts, ILogger logger, CancellationToken ct)
    {
        var scope = selection.Describe();
        var isOrganization = string.Equals(scope.Kind, MapScopeKinds.Organization, StringComparison.Ordinal);
        var isEverything = string.Equals(scope.Kind, MapScopeKinds.Everything, StringComparison.Ordinal);
        var summary = new MapSummaryResponse { Scope = scope };
        foreach (var kind in CountedKinds)
        {
            var count = isOrganization
                ? GenericResult<int?>.Success(null)
                : await CountKind(kind, selection, sources, logger, ct).ConfigureAwait(false);
            if (count.IsFailure) return count.ToNewResult<MapSummaryResponse>();

            Note(summary.Nodes, summary.NotCounted, "nodes", kind, count.Value);
        }

        var platform = await CountPlatformKinds(summary, isEverything, counts, logger, ct).ConfigureAwait(false);
        if (platform.IsFailure) return platform;

        foreach (var kind in OtherKinds)
            Note(summary.Nodes, summary.NotCounted, "nodes", kind, null);

        foreach (var relation in Relations)
            Note(summary.Edges, summary.NotCounted, "edges", relation, null);

        return GenericResult<MapSummaryResponse>.Success(summary);
    }

    // Connections, quality rules and schedules: the platform's own members, counted only when the scope is everything.
    private static async Task<IGenericResult<MapSummaryResponse>> CountPlatformKinds(
        MapSummaryResponse summary, bool isEverything, MapKindCounts counts, ILogger logger, CancellationToken ct)
    {
        foreach (var kind in MapKindCounts.Kinds)
        {
            var count = isEverything
                ? await CountRecords(counts.Records(kind, logger), ct).ConfigureAwait(false)
                : GenericResult<int?>.Success(null);
            if (count.IsFailure) return count.ToNewResult<MapSummaryResponse>();

            Note(summary.Nodes, summary.NotCounted, "nodes", kind, count.Value);
        }

        return GenericResult<MapSummaryResponse>.Success(summary);
    }

    private static Task<IGenericResult<int?>> CountKind(
        string kind, MapSelection selection, MapSources sources, ILogger logger, CancellationToken ct)
    {
        if (selection.Held(kind) is { } held)
            return Task.FromResult<IGenericResult<int?>>(GenericResult<int?>.Success(held.Distinct().Count()));

        if (string.Equals(kind, MapKinds.DataStore, StringComparison.Ordinal))
            return Total(sources.DataStores, ct);

        return CountRecords(
            string.Equals(kind, MapKinds.DataSet, StringComparison.Ordinal)
                ? sources.DataSetRecords(logger)
                : sources.PipelineRecords(logger),
            ct);
    }

    private static Task<IGenericResult<int?>> CountRecords(IGenericResult<IDomainRecordProvider<DomainConfiguration>> records, CancellationToken ct)
        => records.IsFailure
            ? Task.FromResult(records.ToNewResult<int?>())
            : Total(records.Value!, ct);

    // One query for the domain table's total: a page of one row is asked for, and only its total is read.
    private static async Task<IGenericResult<int?>> Total<TDomain>(IDomainRecordProvider<TDomain> records, CancellationToken ct)
        where TDomain : DomainConfiguration
    {
        var page = await records.Get(new DomainQuery(null, null, 0, 1, true), ct).ConfigureAwait(false);
        return page.IsFailure
            ? page.ToNewResult<int?>()
            : GenericResult<int?>.Success(page.Value!.Total);
    }

    private static void Note(IDictionary<string, int?> counts, List<string> notCounted, string section, string key, int? count)
    {
        counts[key] = count;
        if (count is null) notCounted.Add(section + "." + key);
    }
}
