using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// One kind of pipeline event a host can relay to the host that serves the hub (FDW-3773).
/// </summary>
public interface IPipelineEventKind : ITypeOption<int, IPipelineEventKind>
{
    /// <summary>Broadcasts <paramref name="request"/> through the host's own broadcaster.</summary>
    /// <param name="broadcaster">The broadcaster that sends to the hub groups.</param>
    /// <param name="request">The relayed event.</param>
    /// <returns>
    /// <see langword="false"/> with nothing sent when <paramref name="request"/> lacks the body this
    /// kind needs; <see langword="true"/> once the broadcast has completed.
    /// </returns>
    Task<bool> Relay(IPipelineStatusBroadcaster broadcaster, PipelineEventRequest request);
}
