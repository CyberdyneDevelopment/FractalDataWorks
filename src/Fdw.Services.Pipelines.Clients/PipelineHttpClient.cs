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
        return Get<PipelineDetailResponse>($"pipelines/{name}", cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<PipelineDetailResponse>> CreatePipeline(
        CreatePipelineClientRequest request, CancellationToken cancellationToken = default)
    {
        return Post<CreatePipelineClientRequest, PipelineDetailResponse>("pipelines", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<PipelineDetailResponse>> UpdatePipeline(
        string name, UpdatePipelineClientRequest request, CancellationToken cancellationToken = default)
    {
        return Patch<UpdatePipelineClientRequest, PipelineDetailResponse>($"pipelines/{name}", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<PipelineTypeSummary>>> GetPipelineTypes(CancellationToken cancellationToken = default)
    {
        return GetList<PipelineTypeSummary>("pipelines/types", cancellationToken);
    }
}
