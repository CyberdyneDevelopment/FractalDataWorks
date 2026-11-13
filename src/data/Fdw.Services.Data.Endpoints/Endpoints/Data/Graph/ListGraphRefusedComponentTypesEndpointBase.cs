using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>GET /datasets/graph/component-types/refused</c> (FDW-5455): lists each component kind a Graph data set's graph refuses, with the family it is in and the
/// result code a save answers for it.
/// </summary>
/// <remarks>
/// It is the <c>GraphRefusedComponentKinds</c> collection flattened: every kind of every family, the family's name and the code the family's refusal answers. The
/// allowed kinds are on <c>GET /datasets/graph/component-types</c>, which is unchanged; together the two cover the catalogue. No kind, family or code is typed here.
/// </remarks>
public abstract class ListGraphRefusedComponentTypesEndpointBase : CrudListEndpointBase<GraphRefusedComponentTypePayload>
{
    /// <summary>Initializes a new instance of the <see cref="ListGraphRefusedComponentTypesEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    protected ListGraphRefusedComponentTypesEndpointBase(ILogger<ListGraphRefusedComponentTypesEndpointBase> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/component-types/refused";

    /// <inheritdoc />
    protected override string EndpointSummary => "List the component types a Graph data set's graph refuses";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Lists each component type a Graph data set refuses (destinations, actions and loops) with its family and the result code a save answers for it.";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<GraphRefusedComponentTypePayload>>> LoadItems(CancellationToken ct) =>
        Task.FromResult(GenericResult<List<GraphRefusedComponentTypePayload>>.Success(
            [.. GraphRefusedComponentKinds.All().SelectMany(
                family => family.ComponentTypeNames.Select(name => new GraphRefusedComponentTypePayload
                {
                    Name = name, Family = family.Name, Code = DataServiceResultCodes.ByName(family.ResultCodeName).Code,
                }))]));
}
