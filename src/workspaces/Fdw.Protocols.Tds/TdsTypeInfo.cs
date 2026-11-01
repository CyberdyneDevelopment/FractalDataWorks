using System;
using System.Collections.Generic;

namespace Fdw.Protocols.Tds;

/// <summary>
/// A TYPE_INFO as a client sends it with a parameter (MS-TDS 2.2.5.6): the type and its facets.
/// </summary>
/// <remarks>
/// Each type id is read by its own entry in a table rather than a branch per type, so the set of types
/// a parameter may carry is the table's keys and adding one is one line. A type id the table does not
/// hold is one this server does not read, and the caller refuses the parameter.
/// </remarks>
public sealed class TdsTypeInfo
{
    /// <summary>The <see cref="MaxLength"/> of a (max) type, whose value is sent as partially length-prefixed chunks.</summary>
    public const int Unbounded = -1;

    private static readonly Dictionary<byte, Func<TdsPayloadReader, byte, TdsTypeInfo>> Readers = new()
    {
        [TdsTypeIds.Null] = static (_, id) => Fixed(id, 0),
        [TdsTypeIds.Int1] = static (_, id) => Fixed(id, 1),
        [TdsTypeIds.Bit] = static (_, id) => Fixed(id, 1),
        [TdsTypeIds.Int2] = static (_, id) => Fixed(id, 2),
        [TdsTypeIds.Int4] = static (_, id) => Fixed(id, 4),
        [TdsTypeIds.DateTime4] = static (_, id) => Fixed(id, 4),
        [TdsTypeIds.Float4] = static (_, id) => Fixed(id, 4),
        [TdsTypeIds.Money] = static (_, id) => Fixed(id, 8),
        [TdsTypeIds.DateTime] = static (_, id) => Fixed(id, 8),
        [TdsTypeIds.Float8] = static (_, id) => Fixed(id, 8),
        [TdsTypeIds.Money4] = static (_, id) => Fixed(id, 4),
        [TdsTypeIds.BigInt] = static (_, id) => Fixed(id, 8),
        [TdsTypeIds.UniqueIdentifier] = static (r, id) => Fixed(id, r.ReadByte()),
        [TdsTypeIds.IntN] = static (r, id) => Fixed(id, r.ReadByte()),
        [TdsTypeIds.BitN] = static (r, id) => Fixed(id, r.ReadByte()),
        [TdsTypeIds.FloatN] = static (r, id) => Fixed(id, r.ReadByte()),
        [TdsTypeIds.MoneyN] = static (r, id) => Fixed(id, r.ReadByte()),
        [TdsTypeIds.DateTimeN] = static (r, id) => Fixed(id, r.ReadByte()),
        [TdsTypeIds.DecimalN] = static (r, id) => Numeric(r, id),
        [TdsTypeIds.NumericN] = static (r, id) => Numeric(r, id),
        [TdsTypeIds.LegacyDecimal] = static (r, id) => Numeric(r, id),
        [TdsTypeIds.Numeric] = static (r, id) => Numeric(r, id),
        [TdsTypeIds.DateN] = static (_, id) => Fixed(id, 3),
        [TdsTypeIds.TimeN] = static (r, id) => Scaled(r, id),
        [TdsTypeIds.DateTime2N] = static (r, id) => Scaled(r, id),
        [TdsTypeIds.DateTimeOffsetN] = static (r, id) => Scaled(r, id),
        [TdsTypeIds.BigVarBinary] = static (r, id) => Sized(r, id, collated: false),
        [TdsTypeIds.BigBinary] = static (r, id) => Sized(r, id, collated: false),
        [TdsTypeIds.BigVarChar] = static (r, id) => Sized(r, id, collated: true),
        [TdsTypeIds.BigChar] = static (r, id) => Sized(r, id, collated: true),
        [TdsTypeIds.NVarChar] = static (r, id) => Sized(r, id, collated: true),
        [TdsTypeIds.NChar] = static (r, id) => Sized(r, id, collated: true),
    };

    private TdsTypeInfo(byte typeId, int maxLength, byte precision, byte scale, byte[] collation)
    {
        TypeId = typeId;
        MaxLength = maxLength;
        Precision = precision;
        Scale = scale;
        Collation = collation;
    }

    /// <summary>Gets the type id.</summary>
    public byte TypeId { get; }

    /// <summary>Gets the declared length in bytes, or <see cref="Unbounded"/> for a (max) type.</summary>
    public int MaxLength { get; }

    /// <summary>Gets the precision of a decimal or numeric.</summary>
    public byte Precision { get; }

    /// <summary>Gets the scale of a decimal, numeric, time, datetime2 or datetimeoffset.</summary>
    public byte Scale { get; }

    /// <summary>Gets the five-byte collation of a character type; empty for any other.</summary>
    public byte[] Collation { get; }

    /// <summary>Gets a value indicating whether the value is sent as partially length-prefixed chunks.</summary>
    public bool IsPlp => MaxLength == Unbounded;

    /// <summary>Reads a TYPE_INFO at the reader's position.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeInfo">The type read; for a type this server does not read, only its id.</param>
    /// <returns>True when the type id is one this server reads; the reader's overrun is the caller's to check.</returns>
    public static bool TryRead(TdsPayloadReader reader, out TdsTypeInfo typeInfo)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var typeId = reader.ReadByte();
        if (!Readers.TryGetValue(typeId, out var read))
        {
            typeInfo = new TdsTypeInfo(typeId, 0, 0, 0, []);
            return false;
        }

        typeInfo = read(reader, typeId);
        return true;
    }

    private static TdsTypeInfo Fixed(byte typeId, int length) => new(typeId, length, 0, 0, []);

    private static TdsTypeInfo Numeric(TdsPayloadReader reader, byte typeId)
    {
        var length = reader.ReadByte();
        var precision = reader.ReadByte();
        var scale = reader.ReadByte();
        return new TdsTypeInfo(typeId, length, precision, scale, []);
    }

    private static TdsTypeInfo Scaled(TdsPayloadReader reader, byte typeId)
        => new(typeId, 0, 0, reader.ReadByte(), []);

    private static TdsTypeInfo Sized(TdsPayloadReader reader, byte typeId, bool collated)
    {
        var length = reader.ReadUInt16();
        var collation = collated ? reader.ReadBytes(5) : [];
        return new TdsTypeInfo(typeId, length == TdsTypeIds.MaxLengthMarker ? Unbounded : length, 0, 0, collation);
    }
}
