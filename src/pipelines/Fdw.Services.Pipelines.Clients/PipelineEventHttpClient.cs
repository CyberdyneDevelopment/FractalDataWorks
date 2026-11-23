using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Clients;

/// <summary>
/// HTTP client that hands pipeline events to the host that serves the hub, one at a time (FDW-3773) or a tick's worth in one request (FDW-5249).
/// </summary>
public class PipelineEventHttpClient : ApiClientBase, IPipelineEventClient, IPipelineEventBatchClient
{
    /// <summary>Initializes a new instance of the <see cref="PipelineEventHttpClient"/> class.</summary>
    public PipelineEventHttpClient(HttpClient httpClient, ILogger<PipelineEventHttpClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <inheritdoc />
    public Task<IGenericResult<PipelineEventResponse>> Relay(
        PipelineEventRequest request, CancellationToken cancellationToken = default)
    {
        return Post<PipelineEventRequest, PipelineEventResponse>("pipelines/events", request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IGenericResult<PipelineEventBatchResponse>> RelayBatch(
        PipelineEventBatchRequest request, CancellationToken cancellationToken = default)
    {
        return Post<PipelineEventBatchRequest, PipelineEventBatchResponse>("pipelines/events/batch", request, cancellationToken);
    }
}
