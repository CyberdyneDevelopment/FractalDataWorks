using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>GET /datasets/graph/component-types</c> (FDW-5295): lists the component types a Graph data set's graph allows, so the pipeline builder draws the palette of a data
/// set graph and not of a pipeline.
/// </summary>
/// <remarks>
/// Every <c>ComponentTypes</c> option except those in a family the kind refuses (<c>GraphRefusedComponentKinds</c>: destinations, actions and loops), by the same lookup
/// the check at save uses (<see cref="GraphDataSetRefusals.FamilyOf"/>), so the list and the refusals cannot disagree and a new component type follows its family. No
/// type name is typed here.
/// </remarks>
public abstract class ListGraphComponentTypesEndpointBase : CrudListEndpointBase<GraphComponentTypePayload>
{
    /// <summary>Initializes a new instance of the <see cref="ListGraphComponentTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListGraphComponentTypesEndpointBase(ILogger<ListGraphComponentTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/component-types";

    /// <inheritdoc />
    protected override string EndpointSummary => "List the component types a Graph data set's graph allows";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Lists the component types of a graph pipeline minus those a Graph data set refuses: destinations, actions and loops.";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<GraphComponentTypePayload>>> LoadItems(CancellationToken ct) =>
        Task.FromResult(GenericResult<List<GraphComponentTypePayload>>.Success(
            [.. ComponentTypes.All().Where(type => GraphDataSetRefusals.FamilyOf(type.Name) is null).Select(type => new GraphComponentTypePayload { Name = type.Name })]));
}
