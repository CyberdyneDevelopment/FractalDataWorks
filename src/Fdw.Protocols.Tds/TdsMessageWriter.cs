using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Protocols.Tds.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Protocols.Tds;

/// <summary>
/// Writes whole messages to the client, split into packets of the negotiated size.
/// </summary>
/// <remarks>
/// Before login the packet size is the protocol's own 4,096 bytes (MS-TDS 2.2.3.1.3); the login
/// negotiates another and the connection sets <see cref="PacketSize"/> to it. One writer serves one
/// connection, and only the connection's request loop writes through it.
/// </remarks>
public sealed class TdsMessageWriter
{
    /// <summary>The packet size every connection starts with, before login negotiates its own.</summary>
    public const int InitialPacketSize = 4096;

    /// <summary>The smallest packet size a client may negotiate.</summary>
    public const int SmallestPacketSize = 512;

    /// <summary>The largest packet size a client may negotiate.</summary>
    public const int LargestPacketSize = 32767;

    private const int HeaderLength = 8;

    private readonly Stream _stream;
    private readonly ILogger _logger;
    private int _packetSize = InitialPacketSize;
    private byte _packetId = 1;
    private int _packets;

    /// <summary>Initializes a new instance of the <see cref="TdsMessageWriter"/> class.</summary>
    /// <param name="stream">The stream the client reads from.</param>
    /// <param name="logger">The logger.</param>
    public TdsMessageWriter(Stream stream, ILogger? logger)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets or sets the packet size in bytes, header included.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The size is outside 512..32,767.</exception>
    public int PacketSize
    {
        get => _packetSize;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, SmallestPacketSize, nameof(value));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, LargestPacketSize, nameof(value));
            _packetSize = value;
        }
    }

    /// <summary>Gets or sets the session id this server reports in every packet header.</summary>
    public ushort SessionId { get; set; }

    /// <summary>Writes one message and flushes it.</summary>
    /// <param name="packetType">The packet type every packet carries.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>Success, or the transport failure.</returns>
    public async Task<IGenericResult> Write(byte packetType, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        var written = await WritePart(packetType, payload, endOfMessage: true, cancellationToken).ConfigureAwait(false);
        return written.IsFailure ? written : GenericResult.Success();
    }

    /// <summary>
    /// Writes part of a message: every full packet the payload fills, or all of it when this part ends
    /// the message.
    /// </summary>
    /// <remarks>
    /// A large result goes out while it is still being built: the caller writes what fills whole packets,
    /// keeps the rest, and ends the message with the remainder. Packet ids run on across the parts of one
    /// message and start again with the next.
    /// </remarks>
    /// <param name="packetType">The packet type every packet carries.</param>
    /// <param name="payload">The bytes ready to send.</param>
    /// <param name="endOfMessage">Whether this part ends the message.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>How many payload bytes were written, or the transport failure.</returns>
    public async Task<IGenericResult<int>> WritePart(byte packetType, ReadOnlyMemory<byte> payload, bool endOfMessage, CancellationToken cancellationToken = default)
    {
        var bodySize = _packetSize - HeaderLength;
        var sendable = endOfMessage ? payload.Length : payload.Length - (payload.Length % bodySize);
        var packet = new byte[_packetSize];
        var offset = 0;

        try
        {
            while (offset < sendable || (endOfMessage && offset == 0 && sendable == 0))
            {
                var chunk = Math.Min(bodySize, sendable - offset);
                var last = endOfMessage && offset + chunk >= sendable;
                WriteHeader(packet, packetType, last, chunk + HeaderLength, _packetId);
                payload.Span.Slice(offset, chunk).CopyTo(packet.AsSpan(HeaderLength));
                await _stream.WriteAsync(packet.AsMemory(0, chunk + HeaderLength), cancellationToken).ConfigureAwait(false);
                offset += chunk;
                _packetId = unchecked((byte)(_packetId + 1));
                _packets++;
                if (last)
                    break;
            }

            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            return GenericResult<int>.Failure(TdsProtocolLog.TransportFailed(_logger, ex, "writing to the client"));
        }

        if (endOfMessage)
        {
            TdsProtocolLog.MessageWritten(_logger, packetType, offset, _packets);
            _packetId = 1;
            _packets = 0;
        }

        return GenericResult<int>.Success(offset);
    }

    private void WriteHeader(byte[] packet, byte packetType, bool last, int length, byte packetId)
    {
        packet[0] = packetType;
        packet[1] = last ? TdsPacketStatus.EndOfMessage : TdsPacketStatus.Normal;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), (ushort)length);
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4), SessionId);
        packet[6] = packetId;
        packet[7] = 0;
    }
}
