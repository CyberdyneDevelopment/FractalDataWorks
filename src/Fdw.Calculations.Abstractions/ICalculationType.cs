using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Calculations.Abstractions;

/// <summary>
/// Interface for scalar calculation types that reduce rows to a single value.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface ICalculationType : ITypeOption<int, CalculationTypeBase>
{
    /// <summary>
    /// Performs the calculation on the provided rows, extracting values from the specified column.
    /// </summary>
    /// <param name="rows">The source data rows.</param>
    /// <param name="columnName">The column to extract values from.</param>
    /// <param name="parameter">Optional numeric parameter (e.g., percentile value).</param>
    /// <returns>The calculated scalar result.</returns>
    decimal Calculate(IReadOnlyList<IDataRow> rows, string columnName, double? parameter = null);

    /// <summary>
    /// Returns the SQL expression for this calculation (e.g., "SUM(col)").
    /// </summary>
    /// <param name="columnName">The column name to embed in the expression.</param>
    /// <param name="parameter">Optional numeric parameter.</param>
    /// <returns>A SQL aggregate expression string.</returns>
    string ToSqlExpression(string columnName, double? parameter = null);
}

/// <summary>
/// Generic interface for calculation types that produce a typed output.
/// </summary>
/// <typeparam name="TOut">The output type of the calculation (e.g., decimal, string, bool).</typeparam>
/// <remarks>
/// <para>
/// Enables calculation categories beyond numeric aggregation:
/// <list type="bullet">
///   <item><description><strong>Numeric</strong> — <c>ICalculationType&lt;decimal&gt;</c> (Sum, Avg, Min, Max, Count, Percentile)</description></item>
///   <item><description><strong>String</strong> — <c>ICalculationType&lt;string&gt;</c> (Concat, Format, Substring, Replace)</description></item>
///   <item><description><strong>Boolean</strong> — <c>ICalculationType&lt;bool&gt;</c> (Any, All, Contains, IsNull)</description></item>
///   <item><description><strong>Match</strong> — <c>ICalculationType&lt;IReadOnlyList&lt;string&gt;&gt;</c> (Regex, Pattern, Fuzzy)</description></item>
/// </list>
/// </para>
/// <para>
/// The existing <see cref="ICalculationType"/> is the non-generic scalar form.
/// New calculation categories should implement this generic interface.
/// </para>
/// </remarks>
public interface ICalculationType<TOut>
{
    /// <summary>
    /// Gets the output category for this calculation type (e.g., "Numeric", "String", "Boolean", "Match").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Performs the calculation on the provided values.
    /// </summary>
    /// <param name="values">The input values to calculate on.</param>
    /// <returns>The calculated result.</returns>
    TOut Calculate(IReadOnlyList<object> values);
}
