using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Fdw.Protocols.Tds.Types;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// Resolves a T-SQL data type reference to the conversion SQL Server applies to it.
/// </summary>
/// <remarks>
/// <para>
/// Each T-SQL type is one table entry, so a type is supported or refused by being in the table. A length
/// left unstated takes SQL Server's own rule: 30 in a CAST or CONVERT, 1 in a declaration; a decimal
/// left unstated is decimal(18,0); a time type's scale is 7.
/// </para>
/// <para>
/// The conversions are SQL Server's: a CAST to an integer truncates, to a decimal rounds half away from
/// zero, to a sized string truncates to the size. A value that does not convert is refused with error 245.
/// </para>
/// </remarks>
public static class SqlConversions
{
    private const int CastDefaultLength = 30;
    private const int DeclareDefaultLength = 1;

    private static readonly Dictionary<string, Func<SqlTypeFacets, SqlConversionTarget>> Targets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Int"] = static _ => Whole("int", "Int", int.MinValue, int.MaxValue, static n => (int)n),
        ["BigInt"] = static _ => Whole("bigint", "BigInt", long.MinValue, long.MaxValue, static n => n),
        ["SmallInt"] = static _ => Whole("smallint", "SmallInt", short.MinValue, short.MaxValue, static n => (short)n),
        ["TinyInt"] = static _ => Whole("tinyint", "TinyInt", byte.MinValue, byte.MaxValue, static n => (byte)n),
        ["Bit"] = static _ => new SqlConversionTarget("bit", Type("Bit"), 0, 0, 0, static (v, _) => ToBit(v)),
        ["Decimal"] = static f => Number("decimal", f),
        ["Numeric"] = static f => Number("numeric", f),
        ["Money"] = static _ => Number("money", SqlTypeFacets.Fixed(0, 19, 4)),
        ["SmallMoney"] = static _ => Number("smallmoney", SqlTypeFacets.Fixed(0, 10, 4)),
        ["Float"] = static _ => new SqlConversionTarget("float", Type("Float"), 0, 0, 0, static (v, _) => ToDouble(v)),
        ["Real"] = static _ => new SqlConversionTarget("real", Type("Real"), 0, 0, 0, static (v, _) => ToDouble(v) is double d ? (float)d : null),
        ["DateTime"] = static _ => Moment("datetime", 3),
        ["SmallDateTime"] = static _ => Moment("smalldatetime", 0),
        ["DateTime2"] = static f => Moment("datetime2", f.TimeScale),
        ["Date"] = static _ => new SqlConversionTarget("date", Type("Date"), 0, 0, 0, static (v, _) => ToMoment(v) is DateTime d ? DateOnly.FromDateTime(d) : null),
        ["Time"] = static f => new SqlConversionTarget("time", Type("Time"), 0, 0, f.TimeScale, static (v, _) => ToTime(v)),
        ["DateTimeOffset"] = static f => new SqlConversionTarget("datetimeoffset", Type("DateTimeOffset"), 0, 0, f.TimeScale, static (v, _) => ToOffset(v)),
        ["Char"] = static f => Text("char", f),
        ["VarChar"] = static f => Text("varchar", f),
        ["NChar"] = static f => Text("nchar", f),
        ["NVarChar"] = static f => Text("nvarchar", f),
        ["Text"] = static _ => Text("text", SqlTypeFacets.Fixed(TdsColumnType.Max, 0, 0)),
        ["NText"] = static _ => Text("ntext", SqlTypeFacets.Fixed(TdsColumnType.Max, 0, 0)),
        ["UniqueIdentifier"] = static _ => new SqlConversionTarget("uniqueidentifier", Type("UniqueIdentifier"), 0, 0, 0, static (v, _) => ToGuid(v)),
        ["Binary"] = static f => Binary("binary", f),
        ["VarBinary"] = static f => Binary("varbinary", f),
        ["Sysname"] = static _ => Text("sysname", SqlTypeFacets.Fixed(128, 0, 0)),
    };

    /// <summary>Resolves a data type reference.</summary>
    /// <param name="reference">The reference as parsed.</param>
    /// <param name="inCast">Whether it appears in a CAST or CONVERT, where an unstated length is 30 rather than 1.</param>
    /// <param name="line">The line it was written on.</param>
    /// <param name="logger">The logger a refusal is recorded through.</param>
    /// <returns>The conversion target, or the refusal.</returns>
    public static IGenericResult<SqlConversionTarget> Resolve(DataTypeReference reference, bool inCast, int line, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var name = reference is SqlDataTypeReference sql
            ? sql.SqlDataTypeOption.ToString()
            : reference.Name.BaseIdentifier.Value;

        if (!Targets.TryGetValue(name, out var target))
            return SqlRefusal.Refuse<SqlConversionTarget>(SqlServerErrors.NotSupported($"data type {name}", line), logger);

        IList<Literal> parameters = reference is ParameterizedDataTypeReference parameterized ? parameterized.Parameters : [];
        return SqlTypeFacets.TryRead(parameters, inCast ? CastDefaultLength : DeclareDefaultLength, out var facets)
            ? GenericResult<SqlConversionTarget>.Success(target(facets))
            : SqlRefusal.Refuse<SqlConversionTarget>(SqlServerErrors.NotSupported($"the length or precision given to {name}", line), logger);
    }

    /// <summary>Gets the conversion SQL Server applies to a declared type name, for a parameter declaration.</summary>
    /// <param name="sqlName">The type's name, such as nvarchar.</param>
    /// <param name="facets">Its stated facets.</param>
    /// <param name="target">The conversion target.</param>
    /// <returns>True when the type is one this endpoint converts to.</returns>
    public static bool TryResolve(string sqlName, SqlTypeFacets facets, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SqlConversionTarget? target)
    {
        target = Targets.TryGetValue(sqlName, out var build) ? build(facets) : null;
        return target is not null;
    }

    private static ISqlDataType Type(string option) => SqlDataTypes.ByName(option);

    private static SqlConversionTarget Whole(string sqlName, string option, long min, long max, Func<long, object> box)
        => new(sqlName, Type(option), 0, 0, 0, (v, _) => ToWhole(v) is long n && n >= min && n <= max ? box(n) : null);

    // A decimal with nothing written is SQL Server's decimal(18,0).
    private static SqlConversionTarget Number(string sqlName, SqlTypeFacets facets)
    {
        var precision = facets.Stated ? facets.Precision : (byte)18;
        return new SqlConversionTarget(sqlName, Type("Decimal"), 0, precision, facets.Scale, static (v, t) => ToDecimal(v, t.Precision, t.Scale));
    }

    private static SqlConversionTarget Moment(string sqlName, byte scale)
        => new(sqlName, Type("DateTime2"), 0, 0, scale, static (v, _) => ToMoment(v));

    private static SqlConversionTarget Text(string sqlName, SqlTypeFacets facets)
        => new(sqlName, Type("NVarChar"), facets.Length, 0, 0, static (v, t) => Truncate(SqlValues.ToText(v), t.Length));

    private static SqlConversionTarget Binary(string sqlName, SqlTypeFacets facets)
        => new(sqlName, Type("VarBinary"), facets.Length, 0, 0, static (v, t) => ToBytes(v, t.Length));

    private static string Truncate(string text, int length)
        => length > 0 && text.Length > length ? text[..length] : text;

    private static long? ToWhole(object value) => value switch
    {
        string text => long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null,
        bool flag => flag ? 1 : 0,
        _ => SqlValues.TryNumber(value, out var number) && number is >= long.MinValue and <= long.MaxValue
            ? (long)decimal.Truncate(number)
            : null,
    };

    private static object? ToBit(object value) => value switch
    {
        string text when bool.TryParse(text.Trim(), out var flag) => flag,
        _ => SqlValues.TryNumber(value, out var number) ? number != 0m : null,
    };

    private static decimal? ToDecimal(object value, byte precision, byte scale)
    {
        if (!SqlValues.TryNumber(value, out var number))
            return null;

        var rounded = Math.Round(number, scale, MidpointRounding.AwayFromZero);
        var digits = SqlDecimalType.Magnitude(rounded, Math.Max(scale, (byte)rounded.Scale)).ToString(CultureInfo.InvariantCulture).Length;
        return digits <= precision ? rounded : null;
    }

    private static object? ToDouble(object value) => value switch
    {
        double d => d,
        float f => (double)f,
        _ => SqlValues.TryNumber(value, out var number) ? (double)number : null,
    };

    private static object? ToMoment(object value) => value switch
    {
        DateTime moment => moment,
        DateTimeOffset offset => offset.DateTime,
        DateOnly date => date.ToDateTime(TimeOnly.MinValue),
        string text when DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
        _ => null,
    };

    private static object? ToTime(object value) => value switch
    {
        TimeSpan span => span,
        DateTime moment => moment.TimeOfDay,
        string text when TimeSpan.TryParse(text.Trim(), CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => null,
    };

    private static object? ToOffset(object value) => value switch
    {
        DateTimeOffset offset => offset,
        DateTime moment => new DateTimeOffset(DateTime.SpecifyKind(moment, DateTimeKind.Unspecified), TimeSpan.Zero),
        string text when DateTimeOffset.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
        _ => null,
    };

    private static object? ToGuid(object value) => value switch
    {
        Guid id => id,
        string text when Guid.TryParse(text.Trim(), out var parsed) => parsed,
        _ => null,
    };

    private static byte[]? ToBytes(object value, int length)
    {
        var bytes = value switch
        {
            byte[] raw => raw,
            string text => Encoding.Unicode.GetBytes(text),
            _ => null,
        };

        return bytes is not null && length > 0 && bytes.Length > length ? bytes[..length] : bytes;
    }
}
