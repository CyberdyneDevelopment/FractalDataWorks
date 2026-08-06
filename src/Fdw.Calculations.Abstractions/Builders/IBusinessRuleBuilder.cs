using System;

namespace Fdw.Calculations.Builders;

/// <summary>
/// Fluent builder interface for configuring business rule evaluation.
/// </summary>
/// <typeparam name="TOutput">The output type of the calculation.</typeparam>
public interface IBusinessRuleBuilder<TOutput>
{
    /// <summary>
    /// Adds a conditional rule with a predicate and value.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="trueValue">The value to return if condition is true.</param>
    /// <param name="falseValue">The value to return if condition is false.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IBusinessRuleBuilder<TOutput> When(Func<object, bool> condition, TOutput trueValue, TOutput falseValue);

    /// <summary>
    /// Adds a rule based on a field value comparison.
    /// </summary>
    /// <param name="fieldName">The field to check.</param>
    /// <param name="operator">The comparison operator (e.g., "==", "!=", "&gt;", "&lt;").</param>
    /// <param name="value">The value to compare against.</param>
    /// <param name="resultValue">The value to return if the comparison is true.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IBusinessRuleBuilder<TOutput> WhenFieldEquals(string fieldName, string @operator, object value, TOutput resultValue);

    /// <summary>
    /// Sets the default value to return if no rules match.
    /// </summary>
    /// <param name="defaultValue">The default value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IBusinessRuleBuilder<TOutput> Otherwise(TOutput defaultValue);
}
