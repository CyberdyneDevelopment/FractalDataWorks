using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>Reads one universe as a graph of its resources and the relationships between them.</summary>
/// <remarks>
/// Derives from the get-by-name base rather than standing alone so it inherits the same policy,
/// not-found handling and ETag behaviour as every other single-resource read; only the route and
/// the projection differ.
/// </remarks>
public abstract class GetUniverseMapEndpointBase : CrudGetEndpointBase<UniverseNameRequest, UniverseMapResponse>
{
    private readonly IUniverseConfigurationProvider _provider;

    /// <inheritdoc />
    protected GetUniverseMapEndpointBase(ILogger<GetUniverseMapEndpointBase> logger, IUniverseConfigurationProvider provider) : base(logger)
    {
        _provider = provider;
    }

    /// <summary>Gets the resource name used for policy generation.</summary>
    protected override string ResourceName => "universes";

    /// <inheritdoc />
    protected override string Route => "/universes/{Name}/map";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get a universe's map";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Returns the universe's resources as nodes and its declared relationships as edges.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(UniverseNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<UniverseMapResponse?>> FindByIdentifier(
        UniverseNameRequest request, CancellationToken ct)
    {
        var result = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (result.IsFailure) return result.ToNewResult<UniverseMapResponse?>();

        // Null is a real answer here -- no universe by that name -- and the base turns it into a
        // 404. A universe that exists with nothing attached is a different answer: an empty map,
        // which is a working screen with nothing on it rather than a missing one.
        return result.Value is null
            ? GenericResult<UniverseMapResponse?>.Success(null)
            : GenericResult<UniverseMapResponse?>.Success(ToMap(result.Value));
    }

    /// <summary>Projects a universe onto its graph shape.</summary>
    /// <param name="config">The universe, with its resources and relationships loaded.</param>
    protected static UniverseMapResponse ToMap(UniverseConfiguration config) => new()
    {
        Nodes = config.Resources.Select(r => new UniverseMapNodeDto
        {
            // Why the attachment's Id and not the resource's: a universe can hold the same data set
            // twice under different relationships, and a client keys drag state on the node.
            Id = r.Id.ToString(),
            Label = r.Name,
            NodeType = r.ResourceType,
            Category = r.Relationship,
            X = r.X,
            Y = r.Y,
            Metadata = new Dictionary<string, object>(System.StringComparer.Ordinal)
            {
                ["resourceId"] = r.ResourceId.ToString(),
                ["addedByUserId"] = r.AddedByUserId.ToString(),
            },
        }).ToList(),

        Edges = config.Relationships.Select(rel => new UniverseMapEdgeDto
        {
            Id = rel.Id.ToString(),
            Source = rel.LeftDataSetId.ToString(),
            Target = rel.RightDataSetId.ToString(),
            RelationType = rel.Cardinality,
            Label = rel.Name,

            // Both sides must name a field for the join to be defined. Read from the stored nulls
            // rather than inferred from anything else -- see UniverseMapEdgeDto.IsDefined.
            IsDefined = rel.LeftFieldId.HasValue && rel.RightFieldId.HasValue,

            // The field ids are omitted rather than emitted as empty when unset, so a client cannot
            // read a placeholder as a chosen field.
            Metadata = BuildEdgeMetadata(rel),
        }).ToList(),
    };

    private static Dictionary<string, object> BuildEdgeMetadata(UniverseRelationshipConfiguration rel)
    {
        var metadata = new Dictionary<string, object>(System.StringComparer.Ordinal);
        if (rel.LeftFieldId is { } left) metadata["leftFieldId"] = left.ToString();
        if (rel.RightFieldId is { } right) metadata["rightFieldId"] = right.ToString();
        return metadata;
    }
}
