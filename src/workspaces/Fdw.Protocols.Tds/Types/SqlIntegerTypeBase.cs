using System;
using System.Buffers.Binary;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// Base class for the integer types, written as INTNTYPE of their own width.
/// </summary>
public abstract class SqlIntegerTypeBase : SqlDataTypeBase
{
    private readonly byte _width;
    private readonly Type _clrType;

    /// <summary>Initializes a new instance of the <see cref="SqlIntegerTypeBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="sqlName">The type's T-SQL name.</param>
    /// <param name="systemTypeId">The type's <c>system_type_id</c>.</param>
    /// <param name="width">The value's width in bytes: 1, 2, 4 or 8.</param>
    /// <param name="clrType">The .NET type this one holds.</param>
    protected SqlIntegerTypeBase(string name, string sqlName, byte systemTypeId, byte width, Type clrType)
        : base(name, sqlName, systemTypeId, hasLength: false, hasPrecision: false, hasScale: false)
    {
        _width = width;
        _clrType = clrType;
    }

    /// <inheritdoc/>
    public override bool Holds(Type clrType) => clrType == _clrType;

    /// <inheritdoc/>
    public override short CatalogMaxLength(TdsColumnType column) => _width;

    /// <inheritdoc/>
    public override void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteByte(TdsTypeIds.IntN);
        writer.WriteByte(_width);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
    {
        // A fraction would be rounded away by the conversion, which is writing a different number.
        if (IsFractional(value) || !TryWiden(value, out var whole))
            return false;

        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, whole);
        writer.WriteByte(_width);
        writer.WriteBytes(bytes[.._width]);
        return true;
    }

    private static bool IsFractional(object value) => value switch
    {
        double d => d != Math.Floor(d),
        float f => f != MathF.Floor(f),
        decimal m => m != decimal.Truncate(m),
        _ => false,
    };

    /// <summary>Converts a value to a whole number within this type's range.</summary>
    /// <param name="value">The value.</param>
    /// <param name="whole">The whole number.</param>
    /// <returns>True when the value is a whole number this type can hold.</returns>
    protected abstract bool TryWiden(object value, out long whole);
}
