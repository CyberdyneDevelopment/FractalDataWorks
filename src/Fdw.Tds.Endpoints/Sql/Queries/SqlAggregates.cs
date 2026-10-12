using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>
/// The aggregate functions over a query's rows: COUNT, COUNT_BIG, SUM, AVG, MIN and MAX.
/// </summary>
/// <remarks>
/// <para>
/// NULLs are left out of every aggregate but COUNT(*); over no rows COUNT is 0 and the rest are NULL.
/// The result keeps SQL Server's types: SUM and AVG of integers are int (AVG truncates), of decimals
/// decimal, of floats float; overflowing int is error 8115.
/// </para>
/// <para>
/// The other aggregates T-SQL defines are recognised so that they are never evaluated as scalar functions,
/// and are refused with error 40517.
/// </para>
/// </remarks>
public static class SqlAggregates
{
    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        "COUNT", "COUNT_BIG", "SUM", "AVG", "MIN", "MAX", "STDEV", "STDEVP", "VAR", "VARP",
        "STRING_AGG", "CHECKSUM_AGG", "GROUPING", "GROUPING_ID", "APPROX_COUNT_DISTINCT",
    };

    private static readonly Dictionary<string, Func<IReadOnlyList<object>, object?>> Computers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["COUNT"] = static values => values.Count,
        ["COUNT_BIG"] = static values => (long)values.Count,
        ["SUM"] = static values => Sum(values),
        ["AVG"] = static values => Average(values),
        ["MIN"] = static values => Extreme(values, smallest: true),
        ["MAX"] = static values => Extreme(values, smallest: false),
    };

    /// <summary>Gets a value indicating whether a function name is an aggregate.</summary>
    /// <param name="name">The function's name.</param>
    /// <returns>True for an aggregate.</returns>
    public static bool IsAggregate(string name) => Names.Contains(name);

    /// <summary>Computes one aggregate call over a query's rows.</summary>
    /// <param name="call">The call as parsed.</param>
    /// <param name="rows">One scope per row the aggregate runs over.</param>
    /// <param name="outer">The query's scope, for the refusal.</param>
    /// <returns>The aggregate's value (null for NULL), or the refusal.</returns>
    public static IGenericResult<object?> Compute(FunctionCall call, IReadOnlyList<SqlScope> rows, SqlScope outer)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(outer);
        var name = call.FunctionName.Value;
        if (!Computers.TryGetValue(name, out var compute) || call.Parameters.Count != 1)
            return SqlRefusal.Refuse<object?>(SqlServerErrors.NotSupported(SqlText.Of(call), call.StartLine), outer.Context.Logger);

        if (call.Parameters[0] is ColumnReferenceExpression { ColumnType: ColumnType.Wildcard })
            return GenericResult<object?>.Success(compute(rows.Select(static _ => (object)true).ToList()));

        var values = Values(call, rows);
        if (values.IsFailure)
            return values.ToNewResult<object?>();

        var counted = call.UniqueRowFilter == UniqueRowFilter.Distinct ? Distinct(values.Value!) : values.Value!;
        var result = compute(counted);
        return result is Refused refused
            ? SqlRefusal.Refuse<object?>(new SqlServerMessage(refused.Number, 1, 16, refused.Text, call.StartLine), outer.Context.Logger)
            : GenericResult<object?>.Success(result);
    }

    private static IGenericResult<IReadOnlyList<object>> Values(FunctionCall call, IReadOnlyList<SqlScope> rows)
    {
        var values = new List<object>(rows.Count);
        foreach (var row in rows)
        {
            var value = SqlExpressions.Evaluate(call.Parameters[0], row);
            if (value.IsFailure)
                return value.ToNewResult<IReadOnlyList<object>>();

            if (value.Value is { } present)
                values.Add(present);
        }

        return GenericResult<IReadOnlyList<object>>.Success(values);
    }

    private static List<object> Distinct(IReadOnlyList<object> values)
    {
        var distinct = new List<object>();
        foreach (var value in values)
        {
            if (!distinct.Any(seen => SqlValues.TryCompare(seen, value, out var order) && order == 0))
                distinct.Add(value);
        }

        return distinct;
    }

    private static object? Sum(IReadOnlyList<object> values)
    {
        if (values.Count == 0)
            return null;

        if (values.Any(v => v is double or float))
            return values.All(SqlValues.IsNumber) ? values.Sum(v => Convert.ToDouble(v, System.Globalization.CultureInfo.InvariantCulture)) : NotNumbers("sum");

        if (!values.All(v => v is int or long or short or byte or decimal))
            return NotNumbers("sum");

        var total = values.Aggregate(0m, (sum, v) => sum + Convert.ToDecimal(v, System.Globalization.CultureInfo.InvariantCulture));
        return Typed(total, values);
    }

    private static object? Average(IReadOnlyList<object> values)
    {
        var sum = Sum(values);
        return sum switch
        {
            null or Refused => sum,
            double d => d / values.Count,
            decimal m => m / values.Count,
            long l => l / values.Count,
            _ => Convert.ToInt32(sum, System.Globalization.CultureInfo.InvariantCulture) / values.Count,
        };
    }

    // An integer total keeps the integer type: bigint when any input was, else int, which may overflow.
    private static object Typed(decimal total, IReadOnlyList<object> values)
    {
        if (values.Any(v => v is decimal))
            return total;

        if (values.Any(v => v is long))
            return total is >= long.MinValue and <= long.MaxValue ? (long)total : Overflow("bigint");

        return total is >= int.MinValue and <= int.MaxValue ? (int)total : Overflow("int");
    }

    private static object? Extreme(IReadOnlyList<object> values, bool smallest)
    {
        object? best = null;
        foreach (var value in values)
        {
            if (best is null)
            {
                best = value;
                continue;
            }

            if (!SqlValues.TryCompare(value, best, out var order))
                return new Refused(245, "Conversion failed when comparing values of different types.");

            if ((smallest && order < 0) || (!smallest && order > 0))
                best = value;
        }

        return best;
    }

    private static Refused NotNumbers(string op) => new(8117, $"Operand data type is invalid for {op} operator.");

    private static Refused Overflow(string type) => new(8115, $"Arithmetic overflow error converting expression to data type {type}.");

    // What an aggregate answers when its values cannot be aggregated: the error SQL Server raises for it.
    private sealed record Refused(int Number, string Text);
}
