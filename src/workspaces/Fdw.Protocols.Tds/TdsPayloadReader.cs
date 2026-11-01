using System;
using System.Buffers.Binary;
using System.Text;

namespace Fdw.Protocols.Tds;

/// <summary>
/// Reads the fields of one message payload, in the byte orders TDS uses.
/// </summary>
/// <remarks>
/// A read past the end of the payload does not throw: it answers the type's zero value and sets
/// <see cref="Overrun"/>. A parser reads every field it needs and then checks once, so a truncated or
/// lying packet is refused as malformed in one place rather than raising from wherever it ran out.
/// </remarks>
public sealed class TdsPayloadReader
{
    private readonly byte[] _payload;

    /// <summary>Initializes a new instance of the <see cref="TdsPayloadReader"/> class.</summary>
    /// <param name="payload">The message payload, without packet headers.</param>
    public TdsPayloadReader(byte[] payload)
    {
        _payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    /// <summary>Gets the offset of the next byte to read.</summary>
    public int Position { get; private set; }

    /// <summary>Gets how many bytes are left to read.</summary>
    public int Remaining => _payload.Length - Position;

    /// <summary>Gets the payload's length.</summary>
    public int Length => _payload.Length;

    /// <summary>Gets a value indicating whether any read ran past the end of the payload.</summary>
    public bool Overrun { get; private set; }

    /// <summary>Reads one byte.</summary>
    /// <returns>The byte, or zero past the end.</returns>
    public byte ReadByte() => Take(1) is { } at ? _payload[at] : (byte)0;

    /// <summary>Reads a little-endian unsigned 16-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public ushort ReadUInt16() => Take(2) is { } at ? BinaryPrimitives.ReadUInt16LittleEndian(_payload.AsSpan(at)) : (ushort)0;

    /// <summary>Reads a big-endian unsigned 16-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public ushort ReadUInt16BigEndian() => Take(2) is { } at ? BinaryPrimitives.ReadUInt16BigEndian(_payload.AsSpan(at)) : (ushort)0;

    /// <summary>Reads a little-endian signed 16-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public short ReadInt16() => Take(2) is { } at ? BinaryPrimitives.ReadInt16LittleEndian(_payload.AsSpan(at)) : (short)0;

    /// <summary>Reads a little-endian signed 32-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public int ReadInt32() => Take(4) is { } at ? BinaryPrimitives.ReadInt32LittleEndian(_payload.AsSpan(at)) : 0;

    /// <summary>Reads a little-endian unsigned 32-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public uint ReadUInt32() => Take(4) is { } at ? BinaryPrimitives.ReadUInt32LittleEndian(_payload.AsSpan(at)) : 0u;

    /// <summary>Reads a big-endian unsigned 32-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public uint ReadUInt32BigEndian() => Take(4) is { } at ? BinaryPrimitives.ReadUInt32BigEndian(_payload.AsSpan(at)) : 0u;

    /// <summary>Reads a little-endian signed 64-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public long ReadInt64() => Take(8) is { } at ? BinaryPrimitives.ReadInt64LittleEndian(_payload.AsSpan(at)) : 0L;

    /// <summary>Reads a little-endian unsigned 64-bit integer.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public ulong ReadUInt64() => Take(8) is { } at ? BinaryPrimitives.ReadUInt64LittleEndian(_payload.AsSpan(at)) : 0UL;

    /// <summary>Reads a little-endian 32-bit float.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public float ReadSingle() => Take(4) is { } at ? BinaryPrimitives.ReadSingleLittleEndian(_payload.AsSpan(at)) : 0f;

    /// <summary>Reads a little-endian 64-bit float.</summary>
    /// <returns>The value, or zero past the end.</returns>
    public double ReadDouble() => Take(8) is { } at ? BinaryPrimitives.ReadDoubleLittleEndian(_payload.AsSpan(at)) : 0d;

    /// <summary>Reads a run of bytes.</summary>
    /// <param name="count">How many bytes to read.</param>
    /// <returns>A copy of the bytes, or an empty array past the end.</returns>
    public byte[] ReadBytes(int count) => Take(count) is { } at ? _payload.AsSpan(at, count).ToArray() : [];

    /// <summary>Skips a run of bytes.</summary>
    /// <param name="count">How many bytes to skip.</param>
    public void Skip(int count) => Take(count);

    /// <summary>Reads UTF-16LE text of a known length in characters.</summary>
    /// <param name="characters">How many characters to read.</param>
    /// <returns>The text, or an empty string past the end.</returns>
    public string ReadUnicode(int characters)
        => Take(characters * 2) is { } at ? Encoding.Unicode.GetString(_payload, at, characters * 2) : string.Empty;

    /// <summary>Reads a B_VARCHAR: a one-byte character count, then UTF-16LE text.</summary>
    /// <returns>The text.</returns>
    public string ReadByteLengthUnicode() => ReadUnicode(ReadByte());

    /// <summary>Reads a US_VARCHAR: a two-byte character count, then UTF-16LE text.</summary>
    /// <returns>The text.</returns>
    public string ReadUShortLengthUnicode() => ReadUnicode(ReadUInt16());

    /// <summary>Reads UTF-16LE text at an absolute offset, leaving the position where it was.</summary>
    /// <param name="offset">The offset from the start of the payload.</param>
    /// <param name="characters">How many characters to read.</param>
    /// <returns>The text, or an empty string when it would run past the end.</returns>
    public string ReadUnicodeAt(int offset, int characters)
    {
        var bytes = characters * 2;
        if (characters == 0)
            return string.Empty;

        if (offset < 0 || bytes < 0 || bytes > _payload.Length - offset)
        {
            Overrun = true;
            return string.Empty;
        }

        return Encoding.Unicode.GetString(_payload, offset, bytes);
    }

    /// <summary>Gets a copy of the bytes at an absolute offset, leaving the position where it was.</summary>
    /// <param name="offset">The offset from the start of the payload.</param>
    /// <param name="count">How many bytes.</param>
    /// <returns>The bytes, or an empty array when they would run past the end.</returns>
    public byte[] BytesAt(int offset, int count)
    {
        if (count == 0)
            return [];

        if (offset < 0 || count < 0 || count > _payload.Length - offset)
        {
            Overrun = true;
            return [];
        }

        return _payload.AsSpan(offset, count).ToArray();
    }

    /// <summary>Moves the read position to an absolute offset.</summary>
    /// <param name="offset">The offset from the start of the payload.</param>
    public void Seek(int offset)
    {
        if (offset < 0 || offset > _payload.Length)
        {
            Overrun = true;
            Position = _payload.Length;
            return;
        }

        Position = offset;
    }

    // Answers where a read of `count` bytes starts and advances past it, or marks the overrun. Compared against
    // what remains rather than added to the position: a length the client sent near int.MaxValue would overflow
    // the sum, pass the check and leave the position negative.
    private int? Take(int count)
    {
        if (count < 0 || count > Remaining)
        {
            Overrun = true;
            Position = _payload.Length;
            return null;
        }

        var at = Position;
        Position += count;
        return at;
    }
}
