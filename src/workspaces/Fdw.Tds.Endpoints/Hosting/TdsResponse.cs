using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// The tabular result a request is answered with, sent while it is still being built.
/// </summary>
/// <remarks>
/// Tokens are written into <see cref="Payload"/>; once it holds more than the flush threshold, the whole
/// packets it fills are sent and only the remainder is kept. A large result set therefore never sits in
/// memory twice over, and the client starts reading rows while later ones are still being encoded.
/// </remarks>
public sealed class TdsResponse
{
    private readonly TdsMessageWriter _writer;
    private readonly int _flushThreshold;
    private readonly CancellationToken _connectionToken;

    /// <summary>Initializes a new instance of the <see cref="TdsResponse"/> class.</summary>
    /// <param name="writer">The connection's message writer.</param>
    /// <param name="flushThreshold">How many buffered bytes trigger sending the packets they fill.</param>
    /// <param name="connectionToken">The connection's token, which writes honour.</param>
    public TdsResponse(TdsMessageWriter writer, int flushThreshold, CancellationToken connectionToken)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _flushThreshold = flushThreshold;
        _connectionToken = connectionToken;
        Payload = new TdsPayloadWriter(flushThreshold + writer.PacketSize);
    }

    /// <summary>Gets the payload tokens are written into.</summary>
    public TdsPayloadWriter Payload { get; }

    /// <summary>Gets a value indicating whether part of the response has already been sent.</summary>
    public bool Started { get; private set; }

    /// <summary>Sends the whole packets the buffered tokens fill, once there are enough of them.</summary>
    /// <returns>Success, or the transport failure.</returns>
    public async Task<IGenericResult> Flush()
    {
        if (Payload.Length < _flushThreshold)
            return GenericResult.Success();

        var sent = await _writer.WritePart(TdsPacketTypes.TabularResult, Payload.Written, endOfMessage: false, _connectionToken).ConfigureAwait(false);
        if (sent.IsFailure)
            return sent;

        Payload.Discard(sent.Value);
        Started = true;
        return GenericResult.Success();
    }

    /// <summary>Sends everything still buffered and ends the message.</summary>
    /// <returns>Success, or the transport failure.</returns>
    public async Task<IGenericResult> Complete()
    {
        var sent = await _writer.WritePart(TdsPacketTypes.TabularResult, Payload.Written, endOfMessage: true, _connectionToken).ConfigureAwait(false);
        Payload.Clear();
        Started = false;
        return sent.IsFailure ? sent : GenericResult.Success();
    }
}
