using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The client a host that serves no hub uses to hand a pipeline event to the host that does (FDW-3773).
/// </summary>
public interface IPipelineEventClient
{
    /// <summary>Sends a pipeline event to the host that serves the hub, which broadcasts it to the hub groups.</summary>
    /// <param name="request">The event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IGenericResult<PipelineEventResponse>> Relay(PipelineEventRequest request, CancellationToken cancellationToken = default);
}
