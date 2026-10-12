using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Calculations.Abstractions;

/// <summary>
/// Interface for windowed calculation types that produce one value per input row.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
/// <remarks>
/// Unlike scalar <see cref="ICalculationType"/> which collapses rows to a single value,
/// windowed calculations return a result for every row (e.g., ROW_NUMBER, RANK, running totals).
/// In-memory execution returns an ordered list parallel to the input rows.
/// SQL pushdown uses OVER (ORDER BY ...) window functions for true per-partition results.
/// </remarks>
public interface IWindowedCalculationType : ITypeOption<int, WindowedCalculationTypeBase>
{
    /// <summary>
    /// Performs the windowed calculation on the provided rows, returning one result per row.
    /// </summary>
    /// <param name="rows">The source data rows, in the order they should be processed.</param>
    /// <param name="columnName">The column to operate on.</param>
    /// <param name="parameter">Optional numeric parameter.</param>
    /// <returns>One decimal result per input row, in the same order as <paramref name="rows"/>.</returns>
    IReadOnlyList<decimal> Calculate(IReadOnlyList<IDataRow> rows, string columnName, double? parameter = null);

    /// <summary>
    /// Returns the SQL window function expression for this calculation.
    /// </summary>
    /// <param name="columnName">The column name to embed in the expression.</param>
    /// <param name="parameter">Optional numeric parameter.</param>
    /// <returns>A SQL window function expression string (e.g., "ROW_NUMBER() OVER (ORDER BY col)").</returns>
    string ToSqlExpression(string columnName, double? parameter = null);
}
