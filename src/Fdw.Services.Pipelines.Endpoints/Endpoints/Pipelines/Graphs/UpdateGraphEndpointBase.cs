using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Replaces a graph pipeline whole (FDW-1396): what the request states is the graph, and what it leaves out is gone.
/// </summary>
/// <remarks>
/// A graph is one unit, so there is no patch of one component: the console sends the graph it drew. The judging and the save are
/// the create's.
/// </remarks>
public abstract class UpdateGraphEndpointBase : CrudUpdateEndpointBase<GraphPayload, GraphPayload>
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly StageChainLookup _chains;

    /// <summary>Initializes a new instance of the <see cref="UpdateGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="transformations">Resolves the named transformations a field mapping's chain applies.</param>
    protected UpdateGraphEndpointBase(
        ILogger<UpdateGraphEndpointBase> logger, PipelineServiceConfigurationProvider provider, ITransformationConfigurationProvider transformations) : base(logger)
    {
        _provider = provider;
        _chains = new StageChainLookup(transformations, logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "pipelines";

    /// <inheritdoc />
    protected override string Route => "/pipelines/{Name}/graph";

    /// <inheritdoc />
    protected override HttpVerb UpdateVerb => HttpVerb.PUT;

    /// <inheritdoc />
    protected override string EndpointSummary => "Replace a graph pipeline";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Replaces the components, links and dropped outputs of a graph pipeline with the graph in the body. The graph is refused whole when it cannot run.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(GraphPayload request) => request.Name;

    /// <inheritdoc />
    protected override Task<IGenericResult<GraphPayload?>> FindForUpdate(GraphPayload request, CancellationToken ct) =>
        GraphRead.Find(_provider, request.Name, ct);

    /// <inheritdoc />
    protected override async Task<IGenericResult<GraphPayload>> Update(GraphPayload request, GraphPayload existing, CancellationToken ct)
    {
        var original = await _provider.Get(request.Name, ct).ConfigureAwait(false);
        if (original.IsFailure || original.Value is null)
        {
            return GenericResult<GraphPayload>.Failure(ServicesResultCodes.ByName("ConfigurationNotFound"));
        }

        var graph = GraphPayloadReader.Read(request, original.Value.Id, await GraphChainLookup.Resolve(_chains, request, ct).ConfigureAwait(false));
        if (graph.IsFailure || graph.Value is null)
        {
            return graph.ToNewResult<GraphPayload>();
        }

        return await GraphSave.Save(_provider, graph.Value, Logger, ct).ConfigureAwait(false);
    }
}
