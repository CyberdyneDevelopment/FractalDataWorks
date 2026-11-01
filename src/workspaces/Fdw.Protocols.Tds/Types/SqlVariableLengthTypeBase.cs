using System;
using System.Globalization;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// Base class for the variable-length types: a two-byte length and the bytes for (n), partially
/// length-prefixed chunks for (max) (MS-TDS 2.2.5.2.3).
/// </summary>
/// <remarks>
/// A value longer than the column declares is refused. A client sizes its buffers by the declared
/// length, and sending more than it was told to expect is a different column from the one described.
/// </remarks>
public abstract class SqlVariableLengthTypeBase : SqlDataTypeBase
{
    private const ushort NullLength = 0xFFFF;
    private const ulong PlpNull = ulong.MaxValue;

    private readonly byte _typeId;
    private readonly bool _collated;
    private readonly int _bytesPerUnit;

    /// <summary>Initializes a new instance of the <see cref="SqlVariableLengthTypeBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="sqlName">The type's T-SQL name.</param>
    /// <param name="systemTypeId">The type's <c>system_type_id</c>.</param>
    /// <param name="typeId">The TYPE_INFO type byte.</param>
    /// <param name="collated">Whether the TYPE_INFO carries a collation.</param>
    /// <param name="bytesPerUnit">Bytes per declared unit: 2 for a Unicode character, 1 for a byte.</param>
    protected SqlVariableLengthTypeBase(string name, string sqlName, byte systemTypeId, byte typeId, bool collated, int bytesPerUnit)
        : base(name, sqlName, systemTypeId, hasLength: true, hasPrecision: false, hasScale: false)
    {
        _typeId = typeId;
        _collated = collated;
        _bytesPerUnit = bytesPerUnit;
    }

    /// <inheritdoc/>
    public override string Declaration(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return column.IsMax
            ? $"{SqlName}(max)"
            : string.Create(CultureInfo.InvariantCulture, $"{SqlName}({column.Length})");
    }

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column)
    {
        ArgumentNullException.ThrowIfNull(column);
        return column.IsMax ? (short)-1 : (short)(column.Length * _bytesPerUnit);
    }

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(column);
        writer.WriteByte(_typeId);
        writer.WriteUInt16(column.IsMax ? TdsTypeIds.MaxLengthMarker : (ushort)(column.Length * _bytesPerUnit));
        if (_collated)
        {
            ArgumentNullException.ThrowIfNull(collation);
            writer.WriteBytes(collation.Bytes);
        }
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column)
    {
        if (column.IsMax)
            writer.WriteUInt64(PlpNull);
        else
            writer.WriteUInt16(NullLength);
    }

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        if (!TryEncode(value, out var bytes))
            return false;

        if (!column.IsMax)
        {
            if (bytes.Length > column.Length * _bytesPerUnit)
                return false;

            writer.WriteUInt16((ushort)bytes.Length);
            writer.WriteBytes(bytes);
            return true;
        }

        // One chunk holds the whole value; a zero-length chunk ends it.
        writer.WriteUInt64((ulong)bytes.Length);
        if (bytes.Length > 0)
        {
            writer.WriteUInt32((uint)bytes.Length);
            writer.WriteBytes(bytes);
        }

        writer.WriteUInt32(0);
        return true;
    }

    /// <summary>Encodes a value as the bytes this type carries.</summary>
    /// <param name="value">The value.</param>
    /// <param name="bytes">The encoded value.</param>
    /// <returns>True when the value is one this type holds.</returns>
    protected abstract bool TryEncode(object value, out byte[] bytes);
}
