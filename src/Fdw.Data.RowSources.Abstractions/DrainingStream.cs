using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// A write-only stream that collects bytes synchronously and sends them to its target only when the caller drains it, with an
/// asynchronous write.
/// </summary>
/// <remarks>
/// The byte-level sibling of <see cref="DrainingTextWriter"/>, for formats that are not text (an Excel workbook is a zip). A
/// synchronous write to a response body throws under Kestrel, and a zip writer only writes synchronously, so the writer fills
/// this buffer and the response pump drains it between records: memory holds the bytes pending since the last drain, not the
/// file. <see cref="Flush"/> does nothing for the same reason as <see cref="DrainingTextWriter.Flush"/>.
/// </remarks>
public sealed class DrainingStream : Stream
{
    private readonly Stream _target;
    private readonly int _drainThresholdBytes;
    private byte[] _pending;
    private int _pendingCount;

    /// <summary>Initializes a new instance of the <see cref="DrainingStream"/> class.</summary>
    /// <param name="target">The stream drained into.</param>
    /// <param name="drainThresholdBytes">The pending byte count at which <see cref="DrainIfFull"/> writes; greater than zero. Required, with no default: it is the caller's configuration.</param>
    public DrainingStream(Stream target, int drainThresholdBytes)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        if (drainThresholdBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(drainThresholdBytes), drainThresholdBytes, "The drain threshold must be greater than zero.");
        }

        _drainThresholdBytes = drainThresholdBytes;
        _pending = new byte[drainThresholdBytes];
    }

    /// <summary>Gets the bytes collected since the last drain.</summary>
    public int PendingByteCount => _pendingCount;

    /// <summary>Gets how many bytes have been written to the target so far.</summary>
    public long DrainedByteCount { get; private set; }

    /// <inheritdoc />
    public override bool CanRead => false;

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
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_pendingCount + count > _pending.Length)
        {
            Array.Resize(ref _pending, Math.Max(_pending.Length * 2, _pendingCount + count));
        }

        Buffer.BlockCopy(buffer, offset, _pending, _pendingCount, count);
        _pendingCount += count;
    }

    /// <summary>Writes the pending bytes to the target when at least the threshold has collected.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public ValueTask DrainIfFull(CancellationToken cancellationToken = default)
        => _pendingCount >= _drainThresholdBytes ? Drain(cancellationToken) : default;

    /// <summary>Writes every pending byte to the target.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask Drain(CancellationToken cancellationToken = default)
    {
        if (_pendingCount == 0)
        {
            return;
        }

        await _target.WriteAsync(_pending, 0, _pendingCount, cancellationToken).ConfigureAwait(false);
        DrainedByteCount += _pendingCount;
        _pendingCount = 0;
    }

    /// <summary>Writes every pending byte and flushes the target.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async ValueTask Complete(CancellationToken cancellationToken = default)
    {
        await Drain(cancellationToken).ConfigureAwait(false);
        await _target.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();
}
