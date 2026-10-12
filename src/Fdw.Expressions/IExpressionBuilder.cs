using System;
using System.Linq.Expressions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataContainers.Abstractions;
using Fdw.Results;

namespace Fdw.Expressions;

/// <summary>
/// Builds and compiles LINQ expressions for data operations.
/// </summary>
/// <remarks>
/// IExpressionBuilder provides a fluent API for creating compiled expressions
/// that operate on IDataRow instances. Compiled expressions are cached for reuse
/// to avoid repeated compilation overhead.
/// </remarks>
public interface IExpressionBuilder
{
    /// <summary>
    /// Builds a predicate expression for filtering rows.
    /// </summary>
    /// <param name="schema">The schema describing row structure.</param>
    /// <param name="predicate">The predicate logic as an expression tree.</param>
    /// <returns>Compiled predicate function.</returns>
    /// <remarks>
    /// Example:
    /// <code>
    /// var filter = builder.BuildPredicate(schema,
    ///     row => row.GetValue&lt;decimal&gt;("Price") > 100);
    /// </code>
    /// </remarks>
    Func<IDataRow, bool> BuildPredicate(
        IDataSchema schema,
        Expression<Func<IDataRow, bool>> predicate);

    /// <summary>
    /// Builds a selector expression for transforming rows.
    /// </summary>
    /// <typeparam name="TResult">The result type of the selector.</typeparam>
    /// <param name="schema">The schema describing row structure.</param>
    /// <param name="selector">The selector logic as an expression tree.</param>
    /// <returns>Compiled selector function.</returns>
    Func<IDataRow, TResult> BuildSelector<TResult>(
        IDataSchema schema,
        Expression<Func<IDataRow, TResult>> selector);

    /// <summary>
    /// Builds a field accessor for efficient field value extraction.
    /// </summary>
    /// <typeparam name="TValue">The type of the field value.</typeparam>
    /// <param name="schema">The schema describing row structure.</param>
    /// <param name="fieldName">The field name to access.</param>
    /// <returns>Compiled field accessor.</returns>
    /// <remarks>
    /// Field accessors cache the field ordinal for maximum performance.
    /// They are significantly faster than repeated name-based lookups.
    /// </remarks>
    IFieldAccessor<TValue> BuildFieldAccessor<TValue>(
        IDataSchema schema,
        string fieldName);

    /// <summary>
    /// Builds an aggregation expression.
    /// </summary>
    /// <typeparam name="TResult">The aggregation result type.</typeparam>
    /// <param name="schema">The schema describing row structure.</param>
    /// <param name="aggregator">The aggregation logic.</param>
    /// <returns>Compiled aggregation function.</returns>
    Func<IDataRow[], TResult> BuildAggregation<TResult>(
        IDataSchema schema,
        Expression<Func<IDataRow[], TResult>> aggregator);

    /// <summary>
    /// Builds a join predicate for combining two datasets.
    /// </summary>
    /// <param name="leftSchema">The left dataset schema.</param>
    /// <param name="rightSchema">The right dataset schema.</param>
    /// <param name="joinPredicate">The join condition.</param>
    /// <returns>Compiled join predicate.</returns>
    Func<IDataRow, IDataRow, bool> BuildJoinPredicate(
        IDataSchema leftSchema,
        IDataSchema rightSchema,
        Expression<Func<IDataRow, IDataRow, bool>> joinPredicate);

    /// <summary>
    /// Compiles a formula string into an executable expression.
    /// </summary>
    /// <typeparam name="TResult">The formula result type.</typeparam>
    /// <param name="schema">The schema for field access.</param>
    /// <param name="formula">Formula string (e.g., "Price * Quantity").</param>
    /// <returns>Result containing compiled function or error.</returns>
    /// <remarks>
    /// Formulas support:
    /// - Field access: FieldName or [Field Name]
    /// - Arithmetic: +, -, *, /, %
    /// - Comparison: ==, !=, &lt;, &gt;, &lt;=, &gt;=
    /// - Logical: &amp;&amp;, ||, !
    /// - Functions: SUM(), AVG(), COUNT(), etc.
    /// </remarks>
    IGenericResult<Func<IDataRow, TResult>> CompileFormula<TResult>(
        IDataSchema schema,
        string formula);

    /// <summary>
    /// Clears the expression cache.
    /// </summary>
    /// <remarks>
    /// Call this when schemas change or memory needs to be reclaimed.
    /// </remarks>
    void ClearCache();

    /// <summary>
    /// Gets statistics about compiled expressions.
    /// </summary>
    /// <value>Cache statistics (hit rate, size, etc.).</value>
    IExpressionCacheStatistics Statistics { get; }
}