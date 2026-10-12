using System;
using Fdw.Collections;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// A SQL Server data type this server writes: how it is named in the catalog, how its TYPE_INFO and its
/// values go on the wire, and which .NET values it holds.
/// </summary>
/// <remarks>
/// Each type answers for itself, so writing a column, describing it in <c>sys.columns</c> and declaring
/// it in <c>INFORMATION_SCHEMA.COLUMNS</c> never branch on which type it is.
/// </remarks>
public interface ISqlDataType : ITypeOption<int, ISqlDataType>
{
    /// <summary>Gets the type's T-SQL name, such as int or nvarchar.</summary>
    string SqlName { get; }

    /// <summary>Gets the type's <c>system_type_id</c> in <c>sys.types</c>.</summary>
    byte SystemTypeId { get; }

    /// <summary>Gets a value indicating whether a column of this type declares a length.</summary>
    bool HasLength { get; }

    /// <summary>Gets a value indicating whether a column of this type declares a precision and scale.</summary>
    bool HasPrecision { get; }

    /// <summary>Gets a value indicating whether a column of this type declares a fractional-second scale.</summary>
    bool HasScale { get; }

    /// <summary>Gets a value indicating whether this is the type a .NET value of the given type is written as.</summary>
    /// <param name="clrType">The .NET type.</param>
    /// <returns>True when values of that type are written as this one.</returns>
    bool Holds(Type clrType);

    /// <summary>Gets the type as a column declares it, such as nvarchar(50) or decimal(18,2).</summary>
    /// <param name="column">The column's facets.</param>
    /// <returns>The declaration.</returns>
    string Declaration(TdsColumnType column);

    /// <summary>Gets the column's <c>max_length</c> in <c>sys.columns</c>: bytes, or -1 for a (max) type.</summary>
    /// <param name="column">The column's facets.</param>
    /// <returns>The length in bytes.</returns>
    short CatalogMaxLength(TdsColumnType column);

    /// <summary>Writes the column's TYPE_INFO.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="column">The column's facets.</param>
    /// <param name="collation">The collation a character type carries.</param>
    void WriteTypeInfo(TdsPayloadWriter writer, TdsColumnType column, TdsCollation collation);

    /// <summary>Writes one value of the column, NULL included.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="column">The column's facets.</param>
    /// <param name="value">The value; null for a NULL.</param>
    /// <returns>True when the value was written; false when it cannot be written as this type.</returns>
    bool TryWriteValue(TdsPayloadWriter writer, TdsColumnType column, object? value);
}
