using System;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// Base class for the floating-point types, written as FLTNTYPE of their own width.
/// </summary>
public abstract class SqlFloatTypeBase : SqlDataTypeBase
{
    private readonly byte _width;
    private readonly Type _clrType;

    /// <summary>Initializes a new instance of the <see cref="SqlFloatTypeBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="sqlName">The type's T-SQL name.</param>
    /// <param name="systemTypeId">The type's <c>system_type_id</c>.</param>
    /// <param name="width">The value's width in bytes: 4 or 8.</param>
    /// <param name="clrType">The .NET type this one holds.</param>
    protected SqlFloatTypeBase(string name, string sqlName, byte systemTypeId, byte width, Type clrType)
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
        writer.WriteByte(TdsTypeIds.FloatN);
        writer.WriteByte(_width);
    }

    /// <inheritdoc/>
    public override void WriteNull(TdsPayloadWriter writer, TdsColumnType column) => writer.WriteByte(0);

    /// <inheritdoc/>
    public override bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value)
        => TryWriteNumber(writer, value);

    /// <summary>Writes the value's length byte and the number in this type's width, writing nothing when it does not convert.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="value">The value.</param>
    /// <returns>True when the value is a number this type holds.</returns>
    protected abstract bool TryWriteNumber(TdsPayloadWriter writer, object value);
}
