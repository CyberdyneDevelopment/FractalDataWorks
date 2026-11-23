using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Notifications;

/// <summary>
/// The client the relay of a host that serves no hub sends through (FDW-5249): it hands each event to the host's
/// <see cref="PipelineEventRelayBuffer"/> instead of posting it, and <see cref="PipelineEventRelayFlusher"/> sends what has gathered once a tick.
/// </summary>
/// <remarks>
/// The answer says the event was taken, not that the hub host has it: a failed send is reported once for the tick it belonged to.
/// </remarks>
public sealed class BufferedPipelineEventClient : IPipelineEventClient
{
    private readonly PipelineEventRelayBuffer _buffer;

    /// <summary>Initializes a new instance of the <see cref="BufferedPipelineEventClient"/> class.</summary>
    /// <param name="buffer">The host's buffer.</param>
    public BufferedPipelineEventClient(PipelineEventRelayBuffer buffer)
    {
        _buffer = buffer;
    }

    /// <inheritdoc />
    public Task<IGenericResult<PipelineEventResponse>> Relay(PipelineEventRequest request, CancellationToken cancellationToken = default)
    {
        _buffer.Add(request);
        return Task.FromResult<IGenericResult<PipelineEventResponse>>(
            GenericResult<PipelineEventResponse>.Success(new PipelineEventResponse { Kind = request.Kind }));
    }
}
