using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Protocols.Tds;

/// <summary>
/// A stream that answers bytes already read from the stream beneath it before reading any more.
/// </summary>
/// <remarks>
/// The first byte a client sends decides how the connection proceeds: 0x12 opens a TDS 7 pre-login,
/// 0x16 opens a TDS 8 TLS handshake. The byte is read to decide and then replayed to whatever reads the
/// connection next, so neither path has to know it was looked at. This stream does not own the stream
/// beneath it: the connection that opened the socket disposes it.
/// </remarks>
public sealed class ReplayPrefixStream : Stream
{
    private readonly Stream _inner;
    private readonly byte[] _prefix;
    private int _prefixOffset;

    /// <summary>Initializes a new instance of the <see cref="ReplayPrefixStream"/> class.</summary>
    /// <param name="inner">The connection's stream; the connection keeps ownership of it.</param>
    /// <param name="prefix">The bytes already read from it, to be answered first.</param>
    public ReplayPrefixStream(Stream inner, byte[] prefix)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
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

    /// <inheritdoc />
    public override void Flush() => _inner.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_prefixOffset < _prefix.Length)
            return TakePrefix(buffer.AsSpan(offset, count));

        return _inner.Read(buffer, offset, count);
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_prefixOffset < _prefix.Length)
            return ValueTask.FromResult(TakePrefix(buffer.Span));

        return _inner.ReadAsync(buffer, cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _inner.WriteAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _inner.WriteAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    private int TakePrefix(Span<byte> destination)
    {
        var count = Math.Min(destination.Length, _prefix.Length - _prefixOffset);
        _prefix.AsSpan(_prefixOffset, count).CopyTo(destination);
        _prefixOffset += count;
        return count;
    }
}
