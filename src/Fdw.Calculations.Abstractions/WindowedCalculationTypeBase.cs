using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Calculations.Abstractions;

/// <summary>
/// Base class for all windowed calculation types.
/// Accepts injected funcs for in-memory execution and SQL window function expression building.
/// </summary>
public abstract class WindowedCalculationTypeBase : TypeOptionBase<int, WindowedCalculationTypeBase>, IWindowedCalculationType
{
    private readonly Func<IReadOnlyList<IDataRow>, string, double?, IReadOnlyList<decimal>> _calculate;
    private readonly Func<string, double?, string> _toSql;

    /// <summary>
    /// Protected parameterless constructor for TypeCollection Empty sentinel.
    /// Not for use in application code.
    /// </summary>
    protected WindowedCalculationTypeBase() : base(0, string.Empty, string.Empty)
    {
        _calculate = (_, _, _) => [];
        _toSql = (_, _) => string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowedCalculationTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the windowed calculation type.</param>
    /// <param name="name">The name of the windowed calculation type.</param>
    /// <param name="category">The category (e.g., "Windowed").</param>
    /// <param name="calculate">In-memory execution func: (rows, columnName, parameter) → one decimal per row.</param>
    /// <param name="toSql">SQL window function builder: (columnName, parameter) → SQL string.</param>
    protected WindowedCalculationTypeBase(
        int id,
        string name,
        string category,
        Func<IReadOnlyList<IDataRow>, string, double?, IReadOnlyList<decimal>> calculate,
        Func<string, double?, string> toSql)
        : base(id, name, category)
    {
        _calculate = calculate;
        _toSql = toSql;
    }

    /// <summary>Performs the windowed calculation on the provided rows.</summary>
    public IReadOnlyList<decimal> Calculate(IReadOnlyList<IDataRow> rows, string columnName, double? parameter = null)
        => _calculate(rows, columnName, parameter);

    /// <summary>Returns the SQL window function expression for this calculation.</summary>
    public string ToSqlExpression(string columnName, double? parameter = null)
        => _toSql(columnName, parameter);
}
