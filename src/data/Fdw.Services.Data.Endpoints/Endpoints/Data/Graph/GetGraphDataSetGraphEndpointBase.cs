using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Endpoints.Logging;
using Fdw.Services.Data.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// <c>GET /datasets/graph/{name}/graph</c> (FDW-5295): answers a Graph data set's fields out and its graph in the shape a graph pipeline states one, so the pipeline
/// builder opens it without a second model.
/// </summary>
/// <remarks>
/// The fields out carry their discussion and their worked-out state, and the intended sources are listed. The graph, its sources and its intended sources are answered
/// only to a caller who may read every source (<see cref="GraphDataSetSourceAccess"/>); any other caller has the description, the fields and a coded restriction,
/// never an empty graph. A data set that does not exist is not found; one of another kind is refused with its own code. Nothing is saved.
/// </remarks>
public abstract class GetGraphDataSetGraphEndpointBase : CrudGetEndpointBase<DataSetNameRequest, GraphDataSetGraphPayload>
{
    private readonly IDataSetConfigurationProvider _dataSetProvider;
    private readonly ICategoryConfigurationProvider _categories;
    private readonly GraphDataSetSourceAccess _access;

    /// <summary>Initializes a new instance of the <see cref="GetGraphDataSetGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dataSetProvider">Reads the data set whole.</param>
    /// <param name="categories">Reads the categories a data set sits under, which name the category of the header.</param>
    /// <param name="authorization">Asks the caller's permission to read each kind of source the graph reads.</param>
    protected GetGraphDataSetGraphEndpointBase(
        ILogger<GetGraphDataSetGraphEndpointBase> logger, IDataSetConfigurationProvider dataSetProvider, ICategoryConfigurationProvider categories,
        IFrameworkAuthorizationService authorization) : base(logger)
    {
        _dataSetProvider = dataSetProvider;
        _categories = categories;
        _access = new GraphDataSetSourceAccess(authorization);
    }

    /// <inheritdoc />
    protected override string ResourceName => "datasets";

    /// <inheritdoc />
    protected override string Route => "/datasets/graph/{Name}/graph";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get the graph of a Graph data set";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Answers a Graph data set's fields out (each with its discussion and state), its components, links and layout in the shape a graph pipeline states, and its intended sources. A caller who may not read every source it reads gets the description and the fields and a coded restriction instead of the graph.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(DataSetNameRequest request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<GraphDataSetGraphPayload?>> FindByIdentifier(DataSetNameRequest request, CancellationToken ct)
    {
        DataSetEndpointLog.LoadingDataSet(Logger, request.Name, string.Empty);

        var loaded = await _dataSetProvider.Get(request.Name, ConfigurationLevels.WholeAggregate, ct).ConfigureAwait(false);
        if (loaded.IsFailure) return loaded.ToNewResult<GraphDataSetGraphPayload?>();

        if (loaded.Value is null)
        {
            DataSetEndpointLog.DataSetNotFound(Logger, request.Name);
            return GenericResult<GraphDataSetGraphPayload?>.Success((GraphDataSetGraphPayload?)null);
        }

        if (loaded.Value is not IGraphDataSetConfiguration graph)
        {
            return GenericResult<GraphDataSetGraphPayload?>.Failure(
                DataServiceResultCodes.ByName("GraphDataSetNotGraph"), Logger,
                ResultDetails.Create("dataSet", request.Name, "implementation", loaded.Value.Implementation));
        }

        var header = await HeaderOf(graph, ct).ConfigureAwait(false);
        if (header.IsFailure || header.Value is null) return header.ToNewResult<GraphDataSetGraphPayload?>();

        var mayRead = await _access.MayReadEvery(graph, new ClaimsPrincipalAuthenticationContext(User), ct).ConfigureAwait(false);
        if (mayRead.IsFailure) return mayRead.ToNewResult<GraphDataSetGraphPayload?>();

        return GenericResult<GraphDataSetGraphPayload?>.Success(
            mayRead.Value ? GraphDataSetGraphPayloads.Open(graph, header.Value) : Restricted(graph, header.Value));
    }

    // The header members are those every other read of a data set answers, mapped by the one mapper, with the category named from the tree.
    private async Task<IGenericResult<DataSetDetailResponse>> HeaderOf(IGraphDataSetConfiguration graph, CancellationToken ct)
    {
        var tree = await DataSetCategoryLinks.Tree(_categories, Logger, ct).ConfigureAwait(false);
        return tree.IsFailure || tree.Value is null
            ? tree.ToNewResult<DataSetDetailResponse>()
            : DataSetCategoryLinks.Describe(DataSetQueryHelper.MapToDetail(graph), tree.Value, graph.CategoryId, Logger);
    }

    private GraphDataSetGraphPayload Restricted(IGraphDataSetConfiguration graph, DataSetDetailResponse header)
    {
        var problem = GraphDataSetProblems.Of("GraphDataSetGraphNotReadable", Logger, ResultDetails.Create("dataSet", graph.Name));
        return GraphDataSetGraphPayloads.Restricted(graph, header, new GraphDataSetRestrictionPayload { Code = problem.Code, Message = problem.Message });
    }
}
