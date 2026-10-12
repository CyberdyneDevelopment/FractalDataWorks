using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using Fdw.Protocols.Tds.Types;
using Fdw.Tds.Endpoints.Sql.Queries;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// What the catalog reports about one table column: the numbers <c>sys.columns</c> carries and the ones
/// INFORMATION_SCHEMA.COLUMNS spells out.
/// </summary>
/// <remarks>
/// <para>
/// A column whose declared type has no SQL Server type here is still listed, under the type name its source
/// declared and type id 0, so a client sees the column and why it cannot be read rather than a table that
/// silently lost it.
/// </para>
/// <para>
/// A decimal declares no precision or scale, so <c>sys.columns</c> shows 0 for both and
/// INFORMATION_SCHEMA shows NULL: not yet determined. A query's result settles them from its values.
/// </para>
/// </remarks>
public sealed class ColumnFacts
{
    private static readonly Dictionary<string, (byte Precision, short? Radix, byte Scale)> Numerics = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"] = (10, 10, 0),
        ["bigint"] = (19, 10, 0),
        ["smallint"] = (5, 10, 0),
        ["tinyint"] = (3, 10, 0),
        ["bit"] = (1, null, 0),
        ["float"] = (53, 2, 0),
        ["real"] = (24, 2, 0),
        ["datetime2"] = (27, null, 7),
        ["datetimeoffset"] = (34, null, 7),
        ["time"] = (16, null, 7),
        ["date"] = (10, null, 0),
    };

    private ColumnFacts()
    {
    }

    /// <summary>Gets the <c>system_type_id</c>; 0 for a type with no SQL Server type.</summary>
    public byte SystemTypeId { get; private init; }

    /// <summary>Gets the type's name as INFORMATION_SCHEMA spells it.</summary>
    public string TypeName { get; private init; } = string.Empty;

    /// <summary>Gets <c>max_length</c>: bytes, or -1 for a (max) type.</summary>
    public short MaxLength { get; private init; }

    /// <summary>Gets <c>precision</c>.</summary>
    public byte Precision { get; private init; }

    /// <summary>Gets <c>scale</c>.</summary>
    public byte Scale { get; private init; }

    /// <summary>Gets the collation of a character column; null for any other.</summary>
    public string? CollationName { get; private init; }

    /// <summary>Gets CHARACTER_MAXIMUM_LENGTH: characters or bytes, -1 for (max); null for a type without one.</summary>
    public int? CharacterLength { get; private init; }

    /// <summary>Gets CHARACTER_OCTET_LENGTH; null for a type without one.</summary>
    public int? OctetLength { get; private init; }

    /// <summary>Gets NUMERIC_PRECISION; null for a non-numeric type or an undeclared one.</summary>
    public byte? NumericPrecision { get; private init; }

    /// <summary>Gets NUMERIC_PRECISION_RADIX.</summary>
    public short? NumericRadix { get; private init; }

    /// <summary>Gets NUMERIC_SCALE.</summary>
    public int? NumericScale { get; private init; }

    /// <summary>Gets DATETIME_PRECISION; null for a type without fractional seconds.</summary>
    public short? DateTimePrecision { get; private init; }

    /// <summary>Describes a column.</summary>
    /// <param name="column">The column.</param>
    /// <param name="context">The request, for the collation and the logger.</param>
    /// <returns>The facts.</returns>
    public static ColumnFacts Of(SqlTableColumn column, SqlExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(context);
        var declared = SqlColumnTyping.Declared(column, context.Logger);
        return declared.IsSuccess
            ? Of(declared.Value!, context.Server.Collation)
            : new ColumnFacts { TypeName = column.DeclaredTypeName };
    }

    /// <summary>Describes a column of a known SQL type.</summary>
    /// <param name="type">The column's type.</param>
    /// <param name="collation">The server's collation.</param>
    /// <returns>The facts.</returns>
    public static ColumnFacts Of(TdsColumnType type, TdsCollation collation)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(collation);
        var isText = type.DataType.Holds(typeof(string));
        var (characters, octets) = Lengths(type, isText);
        var numbers = type.DataType.HasPrecision ? DecimalNumbers(type) : OtherNumbers(type);

        return new ColumnFacts
        {
            SystemTypeId = type.DataType.SystemTypeId,
            TypeName = type.DataType.SqlName,
            MaxLength = type.DataType.CatalogMaxLength(type),
            Precision = numbers.Precision,
            Scale = numbers.Scale,
            CollationName = isText ? collation.Name : null,
            CharacterLength = characters,
            OctetLength = octets,
            NumericPrecision = numbers.NumericPrecision,
            NumericRadix = numbers.Radix,
            NumericScale = numbers.NumericScale,
            DateTimePrecision = TimePrecision(type),
        };
    }

    private static (int? Characters, int? Octets) Lengths(TdsColumnType type, bool isText)
    {
        if (!type.DataType.HasLength)
            return (null, null);

        var units = type.IsMax ? -1 : type.Length;
        return (units, isText && units > 0 ? units * 2 : units);
    }

    // An undeclared decimal (precision 0) is 0 in sys.columns and NULL in INFORMATION_SCHEMA.
    private static Numbers DecimalNumbers(TdsColumnType type)
    {
        var declared = type.Precision > 0;
        return new Numbers(type.Precision, type.Scale, declared ? type.Precision : null, 10, declared ? type.Scale : null);
    }

    private static Numbers OtherNumbers(TdsColumnType type)
    {
        if (!Numerics.TryGetValue(type.DataType.SqlName, out var known))
            return new Numbers(0, 0, null, null, null);

        var scale = type.DataType.HasScale ? type.Scale : known.Scale;
        return known.Radix is { } radix
            ? new Numbers(known.Precision, scale, known.Precision, radix, 0)
            : new Numbers(known.Precision, scale, null, null, null);
    }

    private static short? TimePrecision(TdsColumnType type)
    {
        if (type.DataType.HasScale)
            return type.Scale;

        return string.Equals(type.DataType.SqlName, "date", StringComparison.Ordinal) ? (short)0 : null;
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Numbers(byte Precision, byte Scale, byte? NumericPrecision, short? Radix, int? NumericScale);
}
