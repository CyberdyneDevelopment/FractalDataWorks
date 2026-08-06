using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Calculations.Abstractions;

/// <summary>
/// Base class for all scalar calculation types.
/// Accepts injected funcs for in-memory execution and SQL pushdown expression building.
/// </summary>
public abstract class CalculationTypeBase : TypeOptionBase<int, CalculationTypeBase>, ICalculationType
{
    private readonly Func<IReadOnlyList<IDataRow>, string, double?, decimal> _calculate;
    private readonly Func<string, double?, string> _toSql;

    /// <summary>
    /// Protected parameterless constructor for TypeCollection Empty sentinel.
    /// Not for use in application code.
    /// </summary>
    protected CalculationTypeBase() : base(0, string.Empty, string.Empty)
    {
        _calculate = (_, _, _) => 0m;
        _toSql = (_, _) => string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the calculation type.</param>
    /// <param name="name">The name of the calculation type.</param>
    /// <param name="category">The category (e.g., "Aggregation").</param>
    /// <param name="calculate">In-memory execution func: (rows, columnName, parameter) → decimal.</param>
    /// <param name="toSql">SQL expression builder: (columnName, parameter) → SQL string.</param>
    protected CalculationTypeBase(
        int id,
        string name,
        string category,
        Func<IReadOnlyList<IDataRow>, string, double?, decimal> calculate,
        Func<string, double?, string> toSql)
        : base(id, name, category)
    {
        _calculate = calculate;
        _toSql = toSql;
    }

    /// <summary>Performs the calculation on the provided rows.</summary>
    public decimal Calculate(IReadOnlyList<IDataRow> rows, string columnName, double? parameter = null)
        => _calculate(rows, columnName, parameter);

    /// <summary>Returns the SQL expression for this calculation.</summary>
    public string ToSqlExpression(string columnName, double? parameter = null)
        => _toSql(columnName, parameter);
}
