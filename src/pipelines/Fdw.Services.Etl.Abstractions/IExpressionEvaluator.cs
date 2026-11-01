using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Interface for evaluating expressions in ETL transforms.
/// </summary>
public interface IExpressionEvaluator
{
    /// <summary>
    /// Evaluates an expression and returns the result as a typed value.
    /// </summary>
    /// <typeparam name="T">The expected result type.</typeparam>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">Variable values for the expression.</param>
    /// <returns>The evaluated result.</returns>
    IGenericResult<T> Evaluate<T>(string expression, IReadOnlyDictionary<string, object?> variables);

    /// <summary>
    /// Evaluates a boolean expression (predicate).
    /// </summary>
    /// <param name="expression">The expression to evaluate.</param>
    /// <param name="variables">Variable values for the expression.</param>
    /// <returns>True if the expression evaluates to true, false otherwise.</returns>
    IGenericResult<bool> EvaluatePredicate(string expression, IReadOnlyDictionary<string, object?> variables);

    /// <summary>
    /// Validates an expression without evaluating it.
    /// </summary>
    /// <param name="expression">The expression to validate.</param>
    /// <returns>Success if valid, failure with error details otherwise.</returns>
    IGenericResult ValidateExpression(string expression);
}
