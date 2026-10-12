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
/// Reads whole client messages off a stream: packet headers checked, payloads joined to end-of-message.
/// </summary>
/// <remarks>
/// <para>
/// Every message is bounded by the limit this reader was built with, so a client cannot make the server
/// buffer without end; one that tries is refused before the next packet is read.
/// </para>
/// <para>
/// A message the client marks to be ignored is read and dropped, and the next one is answered. A client
/// that closes between messages is answered with <see cref="TdsMessage.Closed"/>; one that closes inside a
/// message sent a malformed one.
/// </para>
/// </remarks>
public sealed class TdsMessageReader
{
    /// <summary>The smallest limit a reader takes: one packet of the largest size a client may send.</summary>
    public const int SmallestLimit = LargestPacket;

    private const int HeaderLength = 8;
    private const int LargestPacket = 32768;

    private readonly Stream _stream;
    private readonly int _maximumMessageBytes;
    private readonly ILogger _logger;
    private readonly byte[] _header = new byte[HeaderLength];

    /// <summary>Initializes a new instance of the <see cref="TdsMessageReader"/> class.</summary>
    /// <param name="stream">The stream the client's packets arrive on.</param>
    /// <param name="maximumMessageBytes">The largest message this reader accepts.</param>
    /// <param name="logger">The logger.</param>
    public TdsMessageReader(Stream stream, int maximumMessageBytes, ILogger? logger)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumMessageBytes, SmallestLimit, nameof(maximumMessageBytes));
        _maximumMessageBytes = maximumMessageBytes;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Reads the next message the client sent.</summary>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The message, <see cref="TdsMessage.Closed"/> for a clean close, or the failure.</returns>
    public async Task<IGenericResult<TdsMessage>> Read(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var message = await ReadOne(cancellationToken).ConfigureAwait(false);
            if (message.IsFailure)
                return message.ToNewResult<TdsMessage>();

            // A message the client marked to be ignored is dropped, and the next one is the answer.
            if (message.Value is { Ignored: false } kept)
                return GenericResult<TdsMessage>.Success(kept.Message);
        }
    }

    private async Task<IGenericResult<ReadMessage>> ReadOne(CancellationToken cancellationToken)
    {
        using var payload = new MemoryStream();
        byte packetType = 0;
        var resets = false;
        var first = true;

        while (true)
        {
            var header = await Fill(_header, HeaderLength, allowCleanClose: first, cancellationToken).ConfigureAwait(false);
            if (header.IsFailure)
                return header.ToNewResult<ReadMessage>();

            if (!header.Value)
                return GenericResult<ReadMessage>.Success(new ReadMessage(TdsMessage.Closed, Ignored: false));

            var type = _header[0];
            var status = _header[1];
            var length = BinaryPrimitives.ReadUInt16BigEndian(_header.AsSpan(2));

            var checkedHeader = Check(type, status, length, first, packetType, payload.Length);
            if (checkedHeader.IsFailure)
                return checkedHeader.ToNewResult<ReadMessage>();

            if (first)
            {
                packetType = type;
                resets = (status & (TdsPacketStatus.ResetConnection | TdsPacketStatus.ResetConnectionSkipTransaction)) != 0;
                first = false;
            }

            var body = new byte[length - HeaderLength];
            var filled = await Fill(body, body.Length, allowCleanClose: false, cancellationToken).ConfigureAwait(false);
            if (filled.IsFailure)
                return filled.ToNewResult<ReadMessage>();

            await payload.WriteAsync(body, cancellationToken).ConfigureAwait(false);

            if ((status & TdsPacketStatus.EndOfMessage) == 0)
                continue;

            TdsProtocolLog.MessageRead(_logger, packetType, (int)payload.Length);
            var ignored = (status & TdsPacketStatus.Ignore) != 0;
            return GenericResult<ReadMessage>.Success(new ReadMessage(new TdsMessage(packetType, payload.ToArray(), resets), ignored));
        }
    }

    private IGenericResult Check(byte type, byte status, ushort length, bool first, byte messageType, long soFar)
    {
        if (length < HeaderLength || length > LargestPacket)
            return GenericResult.Failure(TdsProtocolLog.Malformed(_logger, "packet header", $"length {length} is outside 8..{LargestPacket}"));

        if (!first && type != messageType)
            return GenericResult.Failure(TdsProtocolLog.Malformed(_logger, "packet header",
                $"type 0x{type:X2} arrived inside a message of type 0x{messageType:X2}"));

        if (soFar + length - HeaderLength > _maximumMessageBytes)
            return GenericResult.Failure(TdsProtocolLog.MessageTooLarge(_logger, soFar + length - HeaderLength, _maximumMessageBytes));

        return GenericResult.Success();
    }

    // Reads exactly `count` bytes. Answers false only for a clean close before the first byte, when allowed.
    private async Task<IGenericResult<bool>> Fill(byte[] buffer, int count, bool allowCleanClose, CancellationToken cancellationToken)
    {
        var read = 0;
        try
        {
            while (read < count)
            {
                var n = await _stream.ReadAsync(buffer.AsMemory(read, count - read), cancellationToken).ConfigureAwait(false);
                if (n == 0)
                {
                    return read == 0 && allowCleanClose
                        ? GenericResult<bool>.Success(false)
                        : GenericResult<bool>.Failure(TdsProtocolLog.Malformed(_logger, "message", "the connection closed inside it"));
                }

                read += n;
            }
        }
        catch (IOException ex)
        {
            return GenericResult<bool>.Failure(TdsProtocolLog.TransportFailed(_logger, ex, "reading from the client"));
        }
        catch (ObjectDisposedException ex)
        {
            return GenericResult<bool>.Failure(TdsProtocolLog.TransportFailed(_logger, ex, "reading from the client"));
        }

        return GenericResult<bool>.Success(true);
    }

    private sealed record ReadMessage(TdsMessage Message, bool Ignored);
}
