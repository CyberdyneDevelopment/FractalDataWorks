using System;
using System.Buffers.Binary;
using System.Text;

namespace Fdw.Protocols.Tds;

/// <summary>
/// Builds one message payload, in the byte orders TDS uses.
/// </summary>
/// <remarks>
/// A token that starts with its own length is written with a placeholder and patched once its body is
/// known, through <see cref="Mark"/> and <see cref="PatchUInt16"/>.
/// </remarks>
public sealed class TdsPayloadWriter
{
    private byte[] _buffer;

    /// <summary>Initializes a new instance of the <see cref="TdsPayloadWriter"/> class.</summary>
    /// <param name="capacity">The initial buffer size in bytes.</param>
    public TdsPayloadWriter(int capacity)
    {
        _buffer = new byte[Math.Max(capacity, 16)];
    }

    /// <summary>Gets how many bytes have been written.</summary>
    public int Length { get; private set; }

    /// <summary>Gets the bytes written so far.</summary>
    public ReadOnlyMemory<byte> Written => _buffer.AsMemory(0, Length);

    /// <summary>Forgets everything written, keeping the buffer.</summary>
    public void Clear() => Length = 0;

    /// <summary>Drops the first bytes written, keeping the rest at the start of the buffer.</summary>
    /// <param name="count">How many bytes were sent and can be dropped.</param>
    public void Discard(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Length, nameof(count));
        _buffer.AsSpan(count, Length - count).CopyTo(_buffer);
        Length -= count;
    }

    /// <summary>Drops everything written after a mark, as when a row that could not be written is taken back.</summary>
    /// <param name="length">The length to cut back to, as <see cref="Mark"/> answered.</param>
    public void TruncateTo(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, Length, nameof(length));
        Length = length;
    }

    /// <summary>Gets the offset the next write lands at, for a later <see cref="PatchUInt16"/>.</summary>
    /// <returns>The current length.</returns>
    public int Mark() => Length;

    /// <summary>Writes one byte.</summary>
    /// <param name="value">The byte.</param>
    public void WriteByte(byte value) => Reserve(1)[0] = value;

    /// <summary>Writes a little-endian unsigned 16-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteUInt16(ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(Reserve(2), value);

    /// <summary>Writes a big-endian unsigned 16-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteUInt16BigEndian(ushort value) => BinaryPrimitives.WriteUInt16BigEndian(Reserve(2), value);

    /// <summary>Writes a little-endian signed 16-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteInt16(short value) => BinaryPrimitives.WriteInt16LittleEndian(Reserve(2), value);

    /// <summary>Writes a little-endian signed 32-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteInt32(int value) => BinaryPrimitives.WriteInt32LittleEndian(Reserve(4), value);

    /// <summary>Writes a little-endian unsigned 32-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteUInt32(uint value) => BinaryPrimitives.WriteUInt32LittleEndian(Reserve(4), value);

    /// <summary>Writes a big-endian unsigned 32-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteUInt32BigEndian(uint value) => BinaryPrimitives.WriteUInt32BigEndian(Reserve(4), value);

    /// <summary>Writes a little-endian signed 64-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteInt64(long value) => BinaryPrimitives.WriteInt64LittleEndian(Reserve(8), value);

    /// <summary>Writes a little-endian unsigned 64-bit integer.</summary>
    /// <param name="value">The value.</param>
    public void WriteUInt64(ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(Reserve(8), value);

    /// <summary>Writes a little-endian 32-bit float.</summary>
    /// <param name="value">The value.</param>
    public void WriteSingle(float value) => BinaryPrimitives.WriteSingleLittleEndian(Reserve(4), value);

    /// <summary>Writes a little-endian 64-bit float.</summary>
    /// <param name="value">The value.</param>
    public void WriteDouble(double value) => BinaryPrimitives.WriteDoubleLittleEndian(Reserve(8), value);

    /// <summary>Writes a run of bytes.</summary>
    /// <param name="bytes">The bytes.</param>
    public void WriteBytes(ReadOnlySpan<byte> bytes) => bytes.CopyTo(Reserve(bytes.Length));

    /// <summary>Writes text as UTF-16LE with no length prefix.</summary>
    /// <param name="text">The text.</param>
    public void WriteUnicode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Encoding.Unicode.GetBytes(text, Reserve(text.Length * 2));
    }

    /// <summary>Writes a B_VARCHAR: a one-byte character count, then UTF-16LE text.</summary>
    /// <param name="text">The text; at most 255 characters.</param>
    /// <exception cref="ArgumentOutOfRangeException">The text is longer than a one-byte count can state.</exception>
    public void WriteByteLengthUnicode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, byte.MaxValue, nameof(text));
        WriteByte((byte)text.Length);
        WriteUnicode(text);
    }

    /// <summary>Writes a US_VARCHAR: a two-byte character count, then UTF-16LE text.</summary>
    /// <param name="text">The text; at most 65,535 characters.</param>
    /// <exception cref="ArgumentOutOfRangeException">The text is longer than a two-byte count can state.</exception>
    public void WriteUShortLengthUnicode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(text.Length, ushort.MaxValue, nameof(text));
        WriteUInt16((ushort)text.Length);
        WriteUnicode(text);
    }

    /// <summary>Overwrites a little-endian unsigned 16-bit integer already written.</summary>
    /// <param name="offset">Where it was written, as <see cref="Mark"/> answered.</param>
    /// <param name="value">The value.</param>
    public void PatchUInt16(int offset, ushort value)
        => BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(offset, 2), value);

    /// <summary>Gets a copy of the bytes written.</summary>
    /// <returns>The payload.</returns>
    public byte[] ToArray() => _buffer.AsSpan(0, Length).ToArray();

    // Grows the buffer when needed and answers the span the next `count` bytes go into.
    private Span<byte> Reserve(int count)
    {
        if (Length + count > _buffer.Length)
        {
            var grown = new byte[Math.Max(_buffer.Length * 2, Length + count)];
            _buffer.AsSpan(0, Length).CopyTo(grown);
            _buffer = grown;
        }

        var span = _buffer.AsSpan(Length, count);
        Length += count;
        return span;
    }
}
