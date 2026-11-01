using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Protocols.Tds;
using Fdw.Protocols.Tds.Types;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// Settles the SQL type a column is announced with: the type its source declares, or the type its values
/// show when the source declares none.
/// </summary>
/// <remarks>
/// <para>
/// A table column's type comes from the .NET type it declares; its length from the declared length, or
/// (max) when it declares none. A date or time keeps .NET's 100-nanosecond precision, scale 7.
/// </para>
/// <para>
/// A decimal declares no precision or scale in a data set's fields, so a result's decimal column takes the
/// smallest precision and scale that holds every value it returns, exactly. A column with no values at all
/// takes SQL Server's own decimal(18,0). The catalog, which describes a table before any row is read, shows
/// such a column's precision and scale as 0: not yet determined.
/// </para>
/// </remarks>
public static class SqlColumnTyping
{
    /// <summary>The longest nvarchar(n); longer is nvarchar(max).</summary>
    public const int LongestNVarChar = 4000;

    /// <summary>The longest varbinary(n); longer is varbinary(max).</summary>
    public const int LongestVarBinary = 8000;

    private const byte TimeScale = 7;

    /// <summary>Describes a table column as its source declares it, before any row is read.</summary>
    /// <param name="column">The column.</param>
    /// <param name="logger">The logger a refusal is recorded through.</param>
    /// <returns>The column type, or the refusal for a type with no SQL Server equivalent.</returns>
    public static IGenericResult<TdsColumnType> Declared(SqlTableColumn column, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(column);
        if (column.ClrType is not { } clr || !SqlDataTypes.ForClrType(clr, out var dataType))
        {
            return SqlRefusal.Refuse<TdsColumnType>(new SqlServerMessage(50000, 2, 16,
                $"Column '{column.Name}' declares type '{column.DeclaredTypeName}', which has no SQL Server type here.", 1), logger);
        }

        return GenericResult<TdsColumnType>.Success(new TdsColumnType(dataType, Length(dataType, column.MaxLength), 0, Scale(dataType)));
    }

    /// <summary>Settles a result column's type from what its source declares and the values it returns.</summary>
    /// <param name="column">The result column.</param>
    /// <param name="values">Every value the column returns.</param>
    /// <param name="logger">The logger a refusal is recorded through.</param>
    /// <returns>The column type, or the refusal.</returns>
    public static IGenericResult<TdsColumnType> Settle(SqlOutputColumn column, IEnumerable<object?> values, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(values);
        var present = values.Where(v => v is not null).Select(v => v!).ToList();

        if (column.Declared is { } declared)
            return declared.DataType.HasPrecision && declared.Precision == 0 ? Decimal(column, present, logger) : GenericResult<TdsColumnType>.Success(declared);

        if (present.Count == 0)
            return GenericResult<TdsColumnType>.Success(new TdsColumnType(SqlDataTypes.ByName("Int"), 0, 0, 0));

        if (!SqlDataTypes.ForClrType(present[0].GetType(), out var dataType))
        {
            return SqlRefusal.Refuse<TdsColumnType>(new SqlServerMessage(50000, 3, 16,
                $"Column '{column.Name}' returns {present[0].GetType().Name} values, which have no SQL Server type here.", 1), logger);
        }

        return dataType.HasPrecision
            ? Decimal(column, present, logger)
            : GenericResult<TdsColumnType>.Success(new TdsColumnType(dataType, Measured(dataType, present), 0, Scale(dataType)));
    }

    private static int Length(ISqlDataType dataType, int? declared)
    {
        if (!dataType.HasLength)
            return 0;

        var longest = dataType.Holds(typeof(string)) ? LongestNVarChar : LongestVarBinary;
        return declared is { } length && length > 0 && length <= longest ? length : TdsColumnType.Max;
    }

    private static byte Scale(ISqlDataType dataType) => dataType.HasScale ? TimeScale : (byte)0;

    // A text or binary column measured from its values: the longest one, or (max) past the type's limit.
    private static int Measured(ISqlDataType dataType, IReadOnlyList<object> values)
    {
        if (!dataType.HasLength)
            return 0;

        var text = dataType.Holds(typeof(string));
        var longest = values.Max(v => v is byte[] bytes ? bytes.Length : SqlValues.ToText(v).Length);
        var limit = text ? LongestNVarChar : LongestVarBinary;
        return longest > limit ? TdsColumnType.Max : Math.Max(longest, 1);
    }

    private static IGenericResult<TdsColumnType> Decimal(SqlOutputColumn column, IReadOnlyList<object> values, ILogger logger)
    {
        var facets = DecimalFacets.Of(values);
        return facets.Precision <= SqlDecimalType.MaxPrecision
            ? GenericResult<TdsColumnType>.Success(new TdsColumnType(SqlDataTypes.ByName("Decimal"), 0, facets.Precision, facets.Scale))
            : SqlRefusal.Refuse<TdsColumnType>(new SqlServerMessage(50000, 4, 16,
                $"Column '{column.Name}' returns decimals spanning {facets.Precision} digits, more than SQL Server's 38.", 1), logger);
    }
}
