using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// A <see cref="TextWriter"/> that accumulates in memory and is drained to a target
/// <see cref="Stream"/> asynchronously, in bounded batches.
/// </summary>
/// <remarks>
/// This is the seam between the record-writer family and a network response body. Every
/// <see cref="IRecordWriter{T}"/> writes synchronously to a <see cref="TextWriter"/>, and a response
/// body cannot be written synchronously — Kestrel refuses a synchronous body write unless
/// <c>AllowSynchronousIO</c> is on. Rather than give every format writer an asynchronous surface,
/// the writers keep writing synchronously into this one's memory buffer and the caller drains that
/// buffer to the stream between records.
/// <para>
/// The caller drives the draining: write a record, then <see cref="Drain(CancellationToken)"/> when
/// the pending count passes the threshold, and <see cref="Complete(CancellationToken)"/> once. That
/// keeps memory at or below the threshold however many records are written, and it is what makes the
/// response arrive progressively instead of at the end.
/// </para>
/// <para>
/// The target stream is NOT owned: disposing this writer never disposes the stream, because a
/// response body outlives the writer that wrote to it.
/// </para>
/// </remarks>
public sealed class DrainingTextWriter : TextWriter
{
    private readonly Stream _target;
    private readonly Encoding _encoding;
    private readonly Encoder _encoder;
    private readonly int _drainThresholdChars;

    private char[] _pending;
    private int _pendingCount;
    private byte[] _byteBuffer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DrainingTextWriter"/> class.
    /// </summary>
    /// <param name="target">The stream drained to. Not owned and never disposed by this writer.</param>
    /// <param name="encoding">The encoding characters are converted with.</param>
    /// <param name="drainThresholdChars">
    /// The pending character count at which <see cref="DrainIfFull(CancellationToken)"/> drains.
    /// </param>
    /// <exception cref="ArgumentNullException">A required argument was null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="drainThresholdChars"/> was not positive.</exception>
    // Why every argument is required with no default: a default threshold or encoding here would be a
    // configuration value invented by the framework. The caller reads both from its own configuration.
    public DrainingTextWriter(Stream target, Encoding encoding, int drainThresholdChars)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        if (drainThresholdChars <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(drainThresholdChars), drainThresholdChars, "The drain threshold must be greater than zero.");
        }

        _encoder = encoding.GetEncoder();
        _drainThresholdChars = drainThresholdChars;
        _pending = new char[drainThresholdChars];
        _byteBuffer = new byte[encoding.GetMaxByteCount(drainThresholdChars)];
    }

    /// <inheritdoc />
    public override Encoding Encoding => _encoding;

    /// <summary>Gets the number of characters written but not yet drained to the target.</summary>
    public int PendingCharCount => _pendingCount;

    /// <summary>Gets the total number of bytes drained to the target so far.</summary>
    public long DrainedByteCount { get; private set; }

    /// <inheritdoc />
    public override void Write(char value)
    {
        EnsureCapacity(1);
        _pending[_pendingCount++] = value;
    }

    /// <inheritdoc />
    public override void Write(char[] buffer, int index, int count)
    {
        if (buffer is null) throw new ArgumentNullException(nameof(buffer));
        EnsureCapacity(count);
        Array.Copy(buffer, index, _pending, _pendingCount, count);
        _pendingCount += count;
    }

    /// <inheritdoc />
    public override void Write(string? value)
    {
        if (value is null) return;
        EnsureCapacity(value.Length);
        value.CopyTo(0, _pending, _pendingCount, value.Length);
        _pendingCount += value.Length;
    }

    /// <summary>
    /// Drains the pending characters to the target when the threshold has been reached.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once any drain has been written and flushed.</returns>
    public ValueTask DrainIfFull(CancellationToken cancellationToken = default)
        => _pendingCount >= _drainThresholdChars ? Drain(cancellationToken) : default;

    /// <summary>
    /// Drains every pending character to the target and flushes it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the bytes have been written and flushed.</returns>
    /// <remarks>
    /// Encoder state is carried between drains, so a surrogate pair split across two drains is
    /// still encoded as one character rather than two replacement characters.
    /// </remarks>
    public ValueTask Drain(CancellationToken cancellationToken = default) => Drain(false, cancellationToken);

    /// <summary>
    /// Drains every pending character and flushes the encoder's own state, ending the output.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the final bytes have been written and flushed.</returns>
    public ValueTask Complete(CancellationToken cancellationToken = default) => Drain(true, cancellationToken);

    // Why Flush() reaches nothing: the pending characters live in memory, and the only path to the
    // target is an asynchronous drain. A synchronous write to a response body is what this class
    // exists to avoid, so flushing synchronously would defeat it rather than serve it.
    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override Task FlushAsync() => Drain(CancellationToken.None).AsTask();

    private async ValueTask Drain(bool flushEncoder, CancellationToken cancellationToken)
    {
        if (_pendingCount == 0 && !flushEncoder)
        {
            return;
        }

        var required = _encoder.GetByteCount(_pending, 0, _pendingCount, flushEncoder);
        if (_byteBuffer.Length < required)
        {
            _byteBuffer = new byte[required];
        }

        var byteCount = _encoder.GetBytes(_pending, 0, _pendingCount, _byteBuffer, 0, flushEncoder);
        _pendingCount = 0;

        if (byteCount > 0)
        {
            await _target.WriteAsync(_byteBuffer, 0, byteCount, cancellationToken).ConfigureAwait(false);
            DrainedByteCount += byteCount;
        }

        await _target.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EnsureCapacity(int additional)
    {
        if (_pendingCount + additional <= _pending.Length)
        {
            return;
        }

        // Why the buffer grows past the threshold instead of draining here: a drain is asynchronous
        // and these writes are synchronous, so the only way to make room mid-record is to grow. The
        // threshold governs when the CALLER drains, between records, where it can await.
        var grown = new char[Math.Max(_pending.Length * 2, _pendingCount + additional)];
        Array.Copy(_pending, grown, _pendingCount);
        _pending = grown;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pending = [];
        _byteBuffer = [];
        base.Dispose(disposing);
    }
}
