using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fdw.Protocols.Tds;

/// <summary>
/// Reads a parameter's value as its TYPE_INFO says it was written (MS-TDS 2.2.5.5), into the .NET
/// value a T-SQL parameter binds to.
/// </summary>
/// <remarks>
/// <para>
/// A NULL reads as null for every type. Each type id is read by its own table entry, as
/// <see cref="TdsTypeInfo"/>'s are. A value this server cannot represent faithfully — a decimal wider than
/// .NET's 96-bit mantissa, varchar text outside ASCII whose code page this server does not decode — is
/// refused with the reason, never read as something near it.
/// </para>
/// </remarks>
public static class TdsValueReader
{
    private static readonly DateTime DateTimeEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    private static readonly Dictionary<byte, Func<TdsPayloadReader, TdsTypeInfo, Read>> Readers = new()
    {
        [TdsTypeIds.Null] = static (_, _) => Read.Of(null),
        [TdsTypeIds.Int1] = static (r, _) => Read.Of(r.ReadByte()),
        [TdsTypeIds.Bit] = static (r, _) => Read.Of(r.ReadByte() != 0),
        [TdsTypeIds.Int2] = static (r, _) => Read.Of(r.ReadInt16()),
        [TdsTypeIds.Int4] = static (r, _) => Read.Of(r.ReadInt32()),
        [TdsTypeIds.BigInt] = static (r, _) => Read.Of(r.ReadInt64()),
        [TdsTypeIds.Float4] = static (r, _) => Read.Of(r.ReadSingle()),
        [TdsTypeIds.Float8] = static (r, _) => Read.Of(r.ReadDouble()),
        [TdsTypeIds.Money] = static (r, _) => Read.Of(Money(r, 8)),
        [TdsTypeIds.Money4] = static (r, _) => Read.Of(Money(r, 4)),
        [TdsTypeIds.DateTime] = static (r, _) => Read.Of(LegacyDateTime(r, 8)),
        [TdsTypeIds.DateTime4] = static (r, _) => Read.Of(LegacyDateTime(r, 4)),
        [TdsTypeIds.IntN] = static (r, _) => Prefixed(r, IntN),
        [TdsTypeIds.BitN] = static (r, _) => Prefixed(r, static (x, _) => Read.Of(x.ReadByte() != 0)),
        [TdsTypeIds.FloatN] = static (r, _) => Prefixed(r, static (x, n) => Read.Of(n == 4 ? x.ReadSingle() : x.ReadDouble())),
        [TdsTypeIds.MoneyN] = static (r, _) => Prefixed(r, static (x, n) => Read.Of(Money(x, n))),
        [TdsTypeIds.DateTimeN] = static (r, _) => Prefixed(r, static (x, n) => Read.Of(LegacyDateTime(x, n))),
        [TdsTypeIds.UniqueIdentifier] = static (r, _) => Prefixed(r, static (x, n) => n == 16 ? Read.Of(new Guid(x.ReadBytes(16))) : Read.Refused($"a uniqueidentifier of {n} bytes")),
        [TdsTypeIds.DecimalN] = static (r, t) => Prefixed(r, (x, n) => Decimal(x, n, t.Scale)),
        [TdsTypeIds.NumericN] = static (r, t) => Prefixed(r, (x, n) => Decimal(x, n, t.Scale)),
        [TdsTypeIds.DateN] = static (r, _) => Prefixed(r, static (x, _) => Read.Of(Date(x))),
        [TdsTypeIds.TimeN] = static (r, t) => Prefixed(r, (x, n) => Read.Of(Time(x, n, t.Scale))),
        [TdsTypeIds.DateTime2N] = static (r, t) => Prefixed(r, (x, n) => Read.Of(DateTime2(x, n, t.Scale))),
        [TdsTypeIds.DateTimeOffsetN] = static (r, t) => Prefixed(r, (x, n) => Read.Of(DateTimeOffsetValue(x, n, t.Scale))),
        [TdsTypeIds.NVarChar] = static (r, t) => Bytes(r, t, static b => Read.Of(Encoding.Unicode.GetString(b))),
        [TdsTypeIds.NChar] = static (r, t) => Bytes(r, t, static b => Read.Of(Encoding.Unicode.GetString(b))),
        [TdsTypeIds.BigVarChar] = static (r, t) => Bytes(r, t, Ascii),
        [TdsTypeIds.BigChar] = static (r, t) => Bytes(r, t, Ascii),
        [TdsTypeIds.BigVarBinary] = static (r, t) => Bytes(r, t, static b => Read.Of(b)),
        [TdsTypeIds.BigBinary] = static (r, t) => Bytes(r, t, static b => Read.Of(b)),
    };

    /// <summary>Reads a value of the given type at the reader's position.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeInfo">The value's TYPE_INFO.</param>
    /// <param name="value">The value read; null for a NULL.</param>
    /// <param name="problem">Why the value cannot be read, when it cannot.</param>
    /// <returns>True when the value was read; the reader's overrun is the caller's to check.</returns>
    public static bool TryRead(TdsPayloadReader reader, TdsTypeInfo typeInfo, out object? value, out string? problem)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(typeInfo);

        if (!Readers.TryGetValue(typeInfo.TypeId, out var read))
        {
            value = null;
            problem = $"type 0x{typeInfo.TypeId:X2} is not one this server reads";
            return false;
        }

        var result = read(reader, typeInfo);
        value = result.Value;
        problem = result.Problem;
        return result.Problem is null;
    }

    // A value whose first byte is its length; zero is NULL.
    private static Read Prefixed(TdsPayloadReader reader, Func<TdsPayloadReader, int, Read> body)
    {
        var length = reader.ReadByte();
        return length == 0 ? Read.Of(null) : body(reader, length);
    }

    private static Read IntN(TdsPayloadReader reader, int length) => length switch
    {
        1 => Read.Of(reader.ReadByte()),
        2 => Read.Of(reader.ReadInt16()),
        4 => Read.Of(reader.ReadInt32()),
        8 => Read.Of(reader.ReadInt64()),
        _ => Read.Refused($"an integer of {length} bytes"),
    };

    // A character or binary value: two-byte length (0xFFFF is NULL), or PLP chunks for a (max) type.
    private static Read Bytes(TdsPayloadReader reader, TdsTypeInfo typeInfo, Func<byte[], Read> decode)
    {
        if (typeInfo.IsPlp)
            return Plp(reader) is { } chunks ? decode(chunks) : Read.Of(null);

        var length = reader.ReadUInt16();
        return length == TdsTypeIds.MaxLengthMarker ? Read.Of(null) : decode(reader.ReadBytes(length));
    }

    // Partially length-prefixed: an eight-byte total (all ones is NULL), then chunks to a zero-length one.
    private static byte[]? Plp(TdsPayloadReader reader)
    {
        if (reader.ReadUInt64() == ulong.MaxValue)
            return null;

        using var buffer = new MemoryStream();
        while (!reader.Overrun)
        {
            var chunk = reader.ReadUInt32();
            if (chunk == 0)
                break;

            buffer.Write(reader.ReadBytes((int)chunk));
        }

        return buffer.ToArray();
    }

    private static Read Ascii(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            if (b > 0x7F)
                return Read.Refused("varchar text outside ASCII; send it as nvarchar");
        }

        return Read.Of(Encoding.ASCII.GetString(bytes));
    }

    // Sign byte (1 = positive), then the magnitude little-endian in the remaining bytes.
    private static Read Decimal(TdsPayloadReader reader, int length, byte scale)
    {
        var positive = reader.ReadByte() == 1;
        var magnitude = reader.ReadBytes(length - 1);
        Span<byte> words = stackalloc byte[16];
        magnitude.AsSpan(0, Math.Min(magnitude.Length, 16)).CopyTo(words);

        if (BinaryPrimitives.ReadUInt32LittleEndian(words[12..]) != 0 || scale > 28)
            return Read.Refused("a decimal wider than .NET's 96-bit mantissa or 28-digit scale");

        return Read.Of(new decimal(
            BinaryPrimitives.ReadInt32LittleEndian(words),
            BinaryPrimitives.ReadInt32LittleEndian(words[4..]),
            BinaryPrimitives.ReadInt32LittleEndian(words[8..]),
            !positive,
            scale));
    }

    private static decimal Money(TdsPayloadReader reader, int length)
    {
        long raw = length == 4
            ? reader.ReadInt32()
            : ((long)reader.ReadInt32() << 32) | reader.ReadUInt32();
        return raw / 10000m;
    }

    private static DateTime LegacyDateTime(TdsPayloadReader reader, int length)
    {
        if (length == 4)
            return DateTimeEpoch.AddDays(reader.ReadUInt16()).AddMinutes(reader.ReadUInt16());

        var days = reader.ReadInt32();
        var threeHundredths = reader.ReadUInt32();
        return DateTimeEpoch.AddDays(days).AddTicks((long)Math.Round(threeHundredths * (TimeSpan.TicksPerSecond / 300d)));
    }

    private static DateTime Date(TdsPayloadReader reader)
    {
        var days = reader.ReadByte() | (reader.ReadByte() << 8) | (reader.ReadByte() << 16);
        return DateTime.MinValue.AddDays(days);
    }

    // Time is (length minus any date/offset bytes) bytes of 10^-scale second units since midnight.
    private static TimeSpan Time(TdsPayloadReader reader, int length, byte scale)
    {
        Span<byte> units = stackalloc byte[8];
        reader.ReadBytes(length).AsSpan().CopyTo(units);
        var value = BinaryPrimitives.ReadInt64LittleEndian(units);
        return TimeSpan.FromTicks(value * TicksPerUnit(scale));
    }

    private static DateTime DateTime2(TdsPayloadReader reader, int length, byte scale)
    {
        var time = Time(reader, length - 3, scale);
        return Date(reader).Add(time);
    }

    private static DateTimeOffset DateTimeOffsetValue(TdsPayloadReader reader, int length, byte scale)
    {
        var utc = DateTime2(reader, length - 2, scale);
        var offset = TimeSpan.FromMinutes(reader.ReadInt16());
        return new DateTimeOffset(DateTime.SpecifyKind(utc + offset, DateTimeKind.Unspecified), offset);
    }

    private static long TicksPerUnit(byte scale)
    {
        long ticks = 1;
        for (var i = scale; i < 7; i++)
            ticks *= 10;

        return ticks;
    }

    private readonly record struct Read(object? Value, string? Problem)
    {
        public static Read Of(object? value) => new(value, null);

        public static Read Refused(string problem) => new(null, problem);
    }
}
