using Fdw.Collections;

namespace Fdw.Etl.Abstractions.Monitoring.ComparisonOperatorOptions;

/// <summary>
/// Interface for comparison operators used in alert rules.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IComparisonOperator : ITypeOption<int, ComparisonOperatorBase>
{
    /// <summary>
    /// Evaluates this comparison operator against two values.
    /// </summary>
    /// <param name="left">The left operand (typically metric value).</param>
    /// <param name="right">The right operand (typically threshold).</param>
    /// <returns>True if the comparison is satisfied, false otherwise.</returns>
    bool Evaluate(double left, double right);

    /// <summary>
    /// Gets the SQL operator symbol (e.g., "=", "!=", ">", etc.).
    /// </summary>
    string SqlOperator { get; }
}
