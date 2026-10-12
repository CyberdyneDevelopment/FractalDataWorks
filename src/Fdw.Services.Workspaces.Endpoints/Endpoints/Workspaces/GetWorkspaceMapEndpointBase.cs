using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Reads one workspace as a graph of its resources and the relationships between them.</summary>
/// <remarks>
/// Derives from the get-by-name base rather than standing alone so it inherits the same policy,
/// not-found handling and ETag behaviour as every other single-resource read; only the route and
/// the projection differ. The variations the workspace owns are not nodes: each data set node carries
/// a count per kind of variation, as tokens (FDW-1027).
/// </remarks>
public abstract class GetWorkspaceMapEndpointBase : CrudGetEndpointBase<WorkspaceNameRequest, WorkspaceMapResponse>
{
    private readonly IWorkspaceConfigurationProvider _provider;
    private readonly IWorkspaceAccessPolicy _access;
    private readonly IDataSetVariationConfigurationProvider _variations;

    /// <inheritdoc />
    protected GetWorkspaceMapEndpointBase(
        ILogger<GetWorkspaceMapEndpointBase> logger,
        IWorkspaceConfigurationProvider provider,
        IWorkspaceAccessPolicy access,
        IDataSetVariationConfigurationProvider variations) : base(logger)
    {
        _provider = provider;
        _access = access;
        _variations = variations;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "workspaces";

    /// <inheritdoc />
    /// <remarks>Viewing a workspace is decided by <see cref="IWorkspaceAccessPolicy.MayView"/>, not by a platform permission alone (FDW-936).</remarks>
    protected override bool ResourceDecidesAccess => true;

    /// <inheritdoc />
    protected override string Route => "/workspaces/{Name}/map";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get a workspace's map";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Returns the workspace's resources as nodes and its declared relationships as edges.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(WorkspaceNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<WorkspaceMapResponse?>> FindByIdentifier(
        WorkspaceNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<WorkspaceMapResponse?>();

        // Null is a real answer here -- no workspace by that name -- and the base turns it into a
        // 404. A workspace that exists with nothing attached is a different answer: an empty map,
        // which is a working screen with nothing on it rather than a missing one.
        if (result.Value is null) return GenericResult<WorkspaceMapResponse?>.Success(null);

        // Hidden answers as absent; an undetermined decision is passed on (see GetWorkspaceEndpointBase).
        var permitted = await _access.MayView(result.Value, ct).ConfigureAwait(false);
        if (permitted.IsFailure)
        {
            return WorkspaceAccessRefusals.Hides(permitted)
                ? GenericResult<WorkspaceMapResponse?>.Success(null)
                : permitted.ToNewResult<WorkspaceMapResponse?>();
        }

        var counts = await VariationCounts(result.Value, ct).ConfigureAwait(false);
        return counts.IsFailure
            ? counts.ToNewResult<WorkspaceMapResponse?>()
            : GenericResult<WorkspaceMapResponse?>.Success(ToMap(result.Value, counts.Value!));
    }

    /// <summary>Counts, per data set, the variations the workspace owns of it by kind.</summary>
    /// <remarks>One domain query for the whole workspace, not one per node.</remarks>
    private async Task<IGenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>> VariationCounts(
        IWorkspaceImplementationConfiguration workspace, CancellationToken ct)
    {
        var owned = await DataSetVariationQueries.OwnedBy(_variations, workspace.Id, ct).ConfigureAwait(false);
        if (owned.IsFailure) return owned.ToNewResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>();

        return GenericResult<IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>>>.Success(
            MapVariationCounts.Of(owned.Value!));
    }

    /// <summary>Projects a workspace onto its graph shape.</summary>
    /// <param name="config">The workspace, with its resources and relationships loaded.</param>
    /// <param name="variationCounts">The variations the workspace owns, counted by kind, per data set's logical id.</param>
    protected static WorkspaceMapResponse ToMap(
        IWorkspaceImplementationConfiguration config,
        IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> variationCounts) => new()
    {
        Nodes = config.Resources.Select(r => new WorkspaceMapNodeDto
        {
            // Why the attachment's Id and not the resource's: a workspace can hold the same data set
            // twice under different relationships, and a client keys drag state on the node.
            Id = r.Id.ToString(),
            Label = r.Name,
            NodeType = r.ResourceType,
            Category = r.Relationship,
            Variations = VariationsOn(r, variationCounts),
            X = r.X,
            Y = r.Y,
            Metadata = new Dictionary<string, object>(System.StringComparer.Ordinal)
            {
                ["resourceId"] = r.ResourceId.ToString(),
                ["addedByUserId"] = r.AddedByUserId.ToString(),
            },
        }).ToList(),

        Edges = config.Relationships.Select(rel => new WorkspaceMapEdgeDto
        {
            Id = rel.Id.ToString(),
            Source = rel.LeftDataSetId.ToString(),
            Target = rel.RightDataSetId.ToString(),
            RelationType = rel.Cardinality,
            Label = rel.Name,

            // Both sides must name a field for the join to be defined. Read from the stored nulls
            // rather than inferred from anything else -- see WorkspaceMapEdgeDto.IsDefined.
            IsDefined = rel.LeftFieldId.HasValue && rel.RightFieldId.HasValue,

            // The field ids are omitted rather than emitted as empty when unset, so a client cannot
            // read a placeholder as a chosen field.
            Metadata = BuildEdgeMetadata(rel),
        }).ToList(),
    };

    /// <summary>The variation tokens of a node: a data set's counts, and none for any other kind of resource.</summary>
    private static IReadOnlyList<WorkspaceMapVariationCountDto> VariationsOn(
        WorkspaceResourceConfiguration resource,
        IReadOnlyDictionary<Guid, IReadOnlyList<WorkspaceMapVariationCountDto>> variationCounts) =>
        string.Equals(resource.ResourceType, "DataSet", StringComparison.Ordinal)
        && variationCounts.TryGetValue(resource.ResourceId, out var counts)
            ? counts
            : [];

    private static Dictionary<string, object> BuildEdgeMetadata(WorkspaceRelationshipConfiguration rel)
    {
        var metadata = new Dictionary<string, object>(System.StringComparer.Ordinal);
        if (rel.LeftFieldId is { } left) metadata["leftFieldId"] = left.ToString();
        if (rel.RightFieldId is { } right) metadata["rightFieldId"] = right.ToString();
        return metadata;
    }
}
