using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Clients;

/// <summary>
/// HTTP client implementation for the pipeline configuration API.
/// </summary>
public class PipelineHttpClient : ApiClientBase, IPipelineClient, IResourceQueryClient<PipelineSummaryResponse, PipelineDetailResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineHttpClient"/> class.
    /// </summary>
    public PipelineHttpClient(HttpClient httpClient, ILogger<PipelineHttpClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<PipelineSummaryResponse>>> List(CancellationToken cancellationToken = default)
    {
        return GetList<PipelineSummaryResponse>("pipelines", cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<PipelineDetailResponse>> Get(string name, CancellationToken cancellationToken = default)
    {
        return Post<GetPipelineClientRequest, PipelineDetailResponse>(
            "pipelines/get", new GetPipelineClientRequest { Name = name }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<PipelineDetailResponse>> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return Post<GetPipelineByIdClientRequest, PipelineDetailResponse>(
            "pipelines/get", new GetPipelineByIdClientRequest { Id = id }, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<GraphPayload>> GetGraph(string name, CancellationToken cancellationToken = default)
    {
        return Get<GraphPayload>($"pipelines/{name}/graph", cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<GraphPayload>> CreateGraph(GraphPayload graph, CancellationToken cancellationToken = default)
    {
        return Post<GraphPayload, GraphPayload>("pipelines/graph", graph, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<GraphPayload>> ReplaceGraph(string name, GraphPayload graph, CancellationToken cancellationToken = default)
    {
        return Put<GraphPayload, GraphPayload>($"pipelines/{name}/graph", graph, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<GraphValidationPayload>> ValidateGraph(GraphPayload graph, CancellationToken cancellationToken = default)
    {
        return Post<GraphPayload, GraphValidationPayload>("pipelines/graph/validate", graph, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<GraphDescriptionPayload>> DescribeGraph(GraphPayload graph, CancellationToken cancellationToken = default)
    {
        return Post<GraphPayload, GraphDescriptionPayload>("pipelines/graph/describe", graph, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult> DeletePipeline(string name, CancellationToken cancellationToken = default)
    {
        return Delete($"pipelines/{name}", cancellationToken);
    }
}
