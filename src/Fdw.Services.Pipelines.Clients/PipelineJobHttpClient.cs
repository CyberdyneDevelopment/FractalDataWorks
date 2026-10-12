using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Clients;

/// <summary>
/// HTTP client implementation for the pipeline job service.
/// </summary>
public class PipelineJobHttpClient : ApiClientBase, IPipelineJobClient, ITriggerClient<TriggerPipelineRequest, TriggerPipelineResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineJobHttpClient"/> class.
    /// </summary>
    public PipelineJobHttpClient(HttpClient httpClient, ILogger<PipelineJobHttpClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    public Task<IGenericResult<TriggerPipelineResponse>> Trigger(
        TriggerPipelineRequest request, CancellationToken cancellationToken = default)
    {
        return Post<TriggerPipelineRequest, TriggerPipelineResponse>("etl/trigger/pipeline", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<TriggerPipelineResponse>> GetStatus(
        Guid executionId, CancellationToken cancellationToken = default)
    {
        return Get<TriggerPipelineResponse>($"etl/jobs/{executionId}/status", cancellationToken);
    }
}
