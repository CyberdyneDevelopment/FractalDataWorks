using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Pipelines;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Reads the dataflow graph inputs once and answers with one layer of the graph. The route and the
/// permission belong to the endpoint that names them; the layer is the only thing that differs.
/// </summary>
public abstract class DataflowLayerEndpointBase : EndpointWithoutRequest<DataflowGraphResponse>
{
    private readonly DataflowGraphConfigurationProvider _provider;
    private readonly PipelineServiceConfigurationProvider _pipelineProvider;
    private readonly ILogger _logger;

    /// <inheritdoc />
    private protected DataflowLayerEndpointBase(
        DataflowGraphConfigurationProvider provider,
        PipelineServiceConfigurationProvider pipelineProvider,
        ILogger logger)
    {
        _provider = provider;
        _pipelineProvider = pipelineProvider;
        _logger = logger;
    }

    /// <summary>Gets whether the layer draws the category path of a data set, which makes the load read the category tree.</summary>
    private protected abstract bool NamesCategories { get; }

    /// <summary>Draws the layer this endpoint serves from the inputs.</summary>
    private protected abstract DataflowGraphResponse BuildLayer(DataflowGraphInputs inputs);

    /// <summary>Builds and returns the layer, rooted at one pipeline when <c>pipelineName</c> is given.</summary>
    public override async Task HandleAsync(CancellationToken ct)
    {
        EndpointLog.ListingResources(_logger, "dataflow graph");

        var loaded = await DataflowGraphInputsLoader.Load(_provider, _pipelineProvider, _logger, NamesCategories, ct).ConfigureAwait(false);
        if (!loaded.IsSuccess)
        {
            await WriteReadFailure(loaded.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var inputs = loaded.Value!;
        var pipelineFilter = Query<string>("pipelineName", isRequired: false);
        if (!string.IsNullOrWhiteSpace(pipelineFilter))
        {
            if (!inputs.HasPipeline(pipelineFilter))
            {
                await WritePipelineNotFound(pipelineFilter, ct).ConfigureAwait(false);
                return;
            }

            inputs = inputs.ForPipeline(pipelineFilter);
        }

        var response = BuildLayer(inputs);
        DataflowGraphConfigurationProviderLog.Loaded(
            _logger, response.Stats.DataSetCount, response.Stats.DataStoreCount, response.Stats.SourceCount);
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    private async Task WritePipelineNotFound(string pipelineName, CancellationToken ct)
    {
        DataflowGraphConfigurationProviderLog.PipelineNotFound(_logger, pipelineName);
        await Send.ResultAsync(TypedResults.Json(new
        {
            errorCode = "NotFound",
            messages = new[] { $"Pipeline '{pipelineName}' was not found." }
        }, statusCode: 404)).ConfigureAwait(false);
    }

    private async Task WriteReadFailure(string? reason, CancellationToken ct)
    {
        await Send.ResultAsync(TypedResults.Json(new
        {
            errorCode = "ReadFailed",
            messages = new[] { $"Reading the dataflow graph failed: {reason}" }
        }, statusCode: 500)).ConfigureAwait(false);
    }
}
