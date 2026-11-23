using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The client a host that serves no hub uses to hand the pipeline events of one tick to the host that does, in one request (FDW-5249). It sits
/// beside <see cref="IPipelineEventClient"/>, which sends one event, so a host on an earlier build keeps working.
/// </summary>
public interface IPipelineEventBatchClient
{
    /// <summary>Sends a batch of pipeline events to the host that serves the hub, which broadcasts each to the hub groups in order.</summary>
    /// <param name="request">The events, oldest first.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<PipelineEventBatchResponse>> RelayBatch(PipelineEventBatchRequest request, CancellationToken cancellationToken = default);
}
