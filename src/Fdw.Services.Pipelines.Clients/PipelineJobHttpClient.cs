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
        // Why: post to the ETL's canonical UnifiedTriggerEndpoint route (POST etl/trigger/pipeline).
        // The earlier "proxy/etl/trigger" target was a phantom: that route is hosted ONLY by the
        // reference-api proxy (inbound), never by the ETL, so scheduler→ETL and api-proxy→ETL both
        // 404'd. Both dispatch paths use this client, so targeting the real ETL route fixes both.
        return Post<TriggerPipelineRequest, TriggerPipelineResponse>("etl/trigger/pipeline", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<TriggerPipelineResponse>> GetStatus(
        Guid executionId, CancellationToken cancellationToken = default)
    {
        return Get<TriggerPipelineResponse>($"etl/jobs/{executionId}/status", cancellationToken);
    }
}
