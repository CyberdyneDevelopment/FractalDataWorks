using System;
using System.Globalization;
using Fdw.Collections;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// Base class for the SQL data types this server writes.
/// </summary>
/// <remarks>
/// A NULL is written the same way for every value a column holds, so the base writes it and an option
/// writes only values. A value whose .NET type is not the column's own is converted invariantly when the
/// conversion is exact; one that does not convert, or would not fit, is refused rather than written as
/// something near it.
/// </remarks>
public abstract class SqlDataTypeBase : TypeOptionBase<SqlDataTypeBase>, ISqlDataType
{
    /// <summary>Initializes a new instance of the <see cref="SqlDataTypeBase"/> class.</summary>
    /// <param name="name">The option's name; must match its TypeOption attribute.</param>
    /// <param name="sqlName">The type's T-SQL name.</param>
    /// <param name="systemTypeId">The type's <c>system_type_id</c>.</param>
    /// <param name="hasLength">Whether a column of this type declares a length.</param>
    /// <param name="hasPrecision">Whether a column of this type declares a precision and scale.</param>
    /// <param name="hasScale">Whether a column of this type declares a fractional-second scale.</param>
    protected SqlDataTypeBase(string name, string sqlName, byte systemTypeId, bool hasLength, bool hasPrecision, bool hasScale)
        : base(name)
    {
        SqlName = sqlName;
        SystemTypeId = systemTypeId;
        HasLength = hasLength;
        HasPrecision = hasPrecision;
        HasScale = hasScale;
    }

    /// <inheritdoc/>
    public string SqlName { get; }

    /// <inheritdoc/>
    public byte SystemTypeId { get; }

    /// <inheritdoc/>
    public bool HasLength { get; }

    /// <inheritdoc/>
    public bool HasPrecision { get; }

    /// <inheritdoc/>
    public bool HasScale { get; }

    /// <inheritdoc/>
    public abstract bool Holds(Type clrType);

    /// <inheritdoc/>
    public virtual string Declaration(TdsColumnType column) => SqlName;

    /// <inheritdoc/>
    public abstract short CatalogMaxLength(TdsColumnType column);

    /// <inheritdoc/>
    public abstract void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation);

    /// <inheritdoc/>
    public bool TryWriteValue(TdsPayloadWriter writer, TdsColumnType column, object? value)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(column);

        if (value is null or DBNull)
        {
            WriteNull(writer, column);
            return true;
        }

        return TryWriteNonNull(writer, column, value);
    }

    /// <summary>Writes a NULL of this type.</summary>
    /// <remarks>The type's own encoding; a caller writes through <see cref="TryWriteValue"/>, which picks this or <see cref="TryWriteNonNull"/>.</remarks>
    /// <param name="writer">The payload being built.</param>
    /// <param name="column">The column's facets.</param>
    public abstract void WriteNull(TdsPayloadWriter writer, TdsColumnType column);

    /// <summary>Writes a value that is not NULL.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="column">The column's facets.</param>
    /// <param name="value">The value.</param>
    /// <returns>True when written; false when it cannot be written as this type.</returns>
    /// <remarks>The type's own encoding; a caller writes through <see cref="TryWriteValue"/>, which handles NULL first.</remarks>
    public abstract bool TryWriteNonNull(TdsPayloadWriter writer, TdsColumnType column, object value);

    /// <summary>Converts a value to the column's .NET type when the conversion is exact.</summary>
    /// <typeparam name="T">The column's .NET type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="converted">The converted value.</param>
    /// <returns>True when the value converted without loss of range.</returns>
    protected static bool TryConvert<T>(object value, out T converted)
    {
        if (value is T same)
        {
            converted = same;
            return true;
        }

        try
        {
            converted = (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            // Not convertible, or out of the type's range: the caller refuses the value and names it.
            converted = default!;
            return false;
        }
    }
}
