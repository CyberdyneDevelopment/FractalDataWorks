using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Protocols.Tds;

/// <summary>
/// The stream a TDS 7 TLS handshake runs over: TLS records carried inside pre-login packets until the
/// handshake completes, raw TLS records after.
/// </summary>
/// <remarks>
/// <para>
/// TDS 7 negotiates encryption in pre-login and then runs the TLS handshake inside packets of type
/// 0x12 (MS-TDS 3.3.5.2). Once the handshake completes both sides drop the packet wrapping and send TLS
/// records directly; every TDS packet after that travels inside TLS. <see cref="System.Net.Security.SslStream"/>
/// sits on this stream, and the connection calls <see cref="EndHandshake"/> when authentication completes.
/// </para>
/// <para>
/// This stream does not own the stream beneath it: the connection that opened the socket disposes it.
/// </para>
/// </remarks>
public sealed class TdsTlsHandshakeStream : Stream
{
    private const int HeaderLength = 8;
    private const int HandshakePacketBody = 4088;

    private readonly Stream _inner;
    private readonly byte[] _header = new byte[HeaderLength];
    private int _packetRemaining;
    private bool _inHandshake = true;

    /// <summary>Initializes a new instance of the <see cref="TdsTlsHandshakeStream"/> class.</summary>
    /// <param name="inner">The connection's stream; the connection keeps ownership of it.</param>
    public TdsTlsHandshakeStream(Stream inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>Ends the packet wrapping: every byte from now on is a raw TLS record.</summary>
    public void EndHandshake() => _inHandshake = false;

    /// <inheritdoc />
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (!_inHandshake)
            return _inner.Read(buffer, offset, count);

        if (_packetRemaining == 0)
        {
            _inner.ReadExactly(_header);
            _packetRemaining = NextPacketBody();
        }

        var read = _inner.Read(buffer, offset, Math.Min(count, _packetRemaining));
        _packetRemaining -= read;
        return read;
    }

    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!_inHandshake)
            return await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

        if (_packetRemaining == 0)
        {
            await _inner.ReadExactlyAsync(_header, cancellationToken).ConfigureAwait(false);
            _packetRemaining = NextPacketBody();
        }

        var read = await _inner.ReadAsync(buffer[..Math.Min(buffer.Length, _packetRemaining)], cancellationToken).ConfigureAwait(false);
        _packetRemaining -= read;
        return read;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (!_inHandshake)
        {
            _inner.Write(buffer, offset, count);
            return;
        }

        var packet = new byte[HeaderLength + HandshakePacketBody];
        var written = 0;
        byte packetId = 1;
        while (written < count)
        {
            var chunk = WrapPacket(packet, buffer.AsSpan(offset + written, count - written), packetId);
            _inner.Write(packet, 0, chunk + HeaderLength);
            written += chunk;
            packetId = unchecked((byte)(packetId + 1));
        }

        _inner.Flush();
    }

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!_inHandshake)
        {
            await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
            return;
        }

        var packet = new byte[HeaderLength + HandshakePacketBody];
        var offset = 0;
        byte packetId = 1;
        while (offset < buffer.Length)
        {
            var chunk = WrapPacket(packet, buffer.Span[offset..], packetId);
            await _inner.WriteAsync(packet.AsMemory(0, chunk + HeaderLength), cancellationToken).ConfigureAwait(false);
            offset += chunk;
            packetId = unchecked((byte)(packetId + 1));
        }

        await _inner.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    // The body length of the packet whose header was just read; a packet with no body is not one a
    // TLS handshake can travel in.
    private int NextPacketBody()
    {
        var body = BinaryPrimitives.ReadUInt16BigEndian(_header.AsSpan(2)) - HeaderLength;
        return body > 0
            ? body
            : throw new IOException("A pre-login packet carrying the TLS handshake has no body.");
    }

    // Writes one pre-login packet around the front of `remaining` and answers how much of it went in.
    private static int WrapPacket(byte[] packet, ReadOnlySpan<byte> remaining, byte packetId)
    {
        var chunk = Math.Min(HandshakePacketBody, remaining.Length);
        packet[0] = TdsPacketTypes.PreLogin;
        packet[1] = chunk == remaining.Length ? TdsPacketStatus.EndOfMessage : TdsPacketStatus.Normal;
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(2), (ushort)(chunk + HeaderLength));
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(4), 0);
        packet[6] = packetId;
        packet[7] = 0;
        remaining[..chunk].CopyTo(packet.AsSpan(HeaderLength));
        return chunk;
    }
}
