using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Creates a graph pipeline (FDW-1396): components wired by named nodes, saved as stated or refused whole.
/// </summary>
/// <remarks>
/// The graph is judged before anything is written: every component's settings against its kind and the shape of the whole graph
/// by the engine's own check. What only the data can say (that a data set exists) is checked when the pipeline runs.
/// </remarks>
public abstract class CreateGraphEndpointBase : CrudCreateEndpointBase<GraphPayload, GraphPayload>
{
    private readonly PipelineServiceConfigurationProvider _provider;
    private readonly StageChainLookup _chains;

    /// <summary>Initializes a new instance of the <see cref="CreateGraphEndpointBase"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="provider">The pipeline domain's configuration provider.</param>
    /// <param name="transformations">Resolves the named transformations a field mapping's chain applies.</param>
    protected CreateGraphEndpointBase(
        ILogger<CreateGraphEndpointBase> logger, PipelineServiceConfigurationProvider provider, ITransformationConfigurationProvider transformations) : base(logger)
    {
        _provider = provider;
        _chains = new StageChainLookup(transformations, logger);
    }

    /// <inheritdoc />
    protected override string ResourceName => "pipelines";

    /// <inheritdoc />
    protected override string Route => "/pipelines/graph";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a graph pipeline";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Creates a pipeline of components wired by named input and output nodes. The graph is refused whole, naming the component and the setting, when it cannot run.";

    /// <inheritdoc />
    protected override string GetResourceName(GraphPayload request) => request.Name;

    /// <inheritdoc />
    protected override async Task<IGenericResult<bool>> CheckExists(GraphPayload request, CancellationToken ct) =>
        GenericResult<bool>.Success(await PipelineDraft.NameTaken(_provider, request.Name, ct).ConfigureAwait(false));

    /// <inheritdoc />
    protected override async Task<IGenericResult<GraphPayload>> Create(GraphPayload request, CancellationToken ct)
    {
        var graph = GraphPayloadReader.Read(request, Guid.CreateVersion7(), await GraphChainLookup.Resolve(_chains, request, ct).ConfigureAwait(false));
        if (graph.IsFailure || graph.Value is null)
        {
            return graph.ToNewResult<GraphPayload>();
        }

        return await GraphSave.Save(_provider, graph.Value, Logger, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task SendCreatedResponse(GraphPayload detail, CancellationToken ct) => Send.ResponseAsync(detail, 201, ct);
}
