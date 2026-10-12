using System;
using System.Linq;
using System.Linq.Expressions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Concrete implementation of IDataQuery that builds LINQ expression trees.
/// This class captures method calls as expression nodes for later translation.
/// </summary>
/// <typeparam name="TSource">The type of records in the source dataset.</typeparam>
/// <remarks>
/// DataQueryBuilder implements the fluent query interface by capturing each
/// method call as part of an expression tree. The complete expression is then
/// available for translation by connection-specific query translators.
/// This follows the same pattern as Entity Framework's query provider.
/// </remarks>
public sealed class DataQueryBuilder<TSource> : IDataQuery<TSource>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataQueryBuilder{TSource}"/> class.
    /// </summary>
    /// <param name="dataSetName">The name of the dataset being queried.</param>
    /// <param name="expression">The current expression tree, or null for a root query.</param>
    public DataQueryBuilder(string dataSetName, Expression? expression = null)
    {
        DataSetName = dataSetName ?? throw new ArgumentNullException(nameof(dataSetName));
        // For root queries, create a constant representing the queryable source
        // Using QueryableSource instead of 'this' to avoid circular references in ToString()
        QueryExpression = expression ?? Expression.Constant(
            new QueryableSource<TSource>(dataSetName),
            typeof(IQueryable<TSource>));
        SourceType = typeof(TSource);
        ResultType = typeof(TSource);
    }

    /// <summary>
    /// Initializes a new instance with a different result type (for Select operations).
    /// </summary>
    /// <param name="dataSetName">The name of the dataset being queried.</param>
    /// <param name="expression">The current expression tree.</param>
    /// <param name="resultType">The expected result type after transformation.</param>
    internal DataQueryBuilder(string dataSetName, Expression expression, Type resultType)
    {
        DataSetName = dataSetName ?? throw new ArgumentNullException(nameof(dataSetName));
        QueryExpression = expression ?? throw new ArgumentNullException(nameof(expression));
        SourceType = typeof(TSource);
        ResultType = resultType;
    }

    /// <inheritdoc/>
    public Expression QueryExpression { get; }

    /// <inheritdoc/>
    public string DataSetName { get; }

    /// <inheritdoc/>
    public Type ResultType { get; }

    /// <inheritdoc/>
    public Type SourceType { get; }

    /// <inheritdoc/>
    public IDataQuery<TSource> Where(Expression<Func<TSource, bool>> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        // Get a method call expression for Where
        var methodCall = Expression.Call(
            type: typeof(Queryable),
            methodName: nameof(Queryable.Where),
            typeArguments: new[] { typeof(TSource) },
            arguments: new[] { QueryExpression, Expression.Quote(predicate) });

        return new DataQueryBuilder<TSource>(DataSetName, methodCall);
    }

    /// <inheritdoc/>
    public IDataQuery<TResult> Select<TResult>(Expression<Func<TSource, TResult>> selector)
    {
        if (selector == null)
            throw new ArgumentNullException(nameof(selector));

        // Get a method call expression for Select
        var methodCall = Expression.Call(
            type: typeof(Queryable),
            methodName: nameof(Queryable.Select),
            typeArguments: new[] { typeof(TSource), typeof(TResult) },
            arguments: new[] { QueryExpression, Expression.Quote(selector) });

        return new DataQueryBuilder<TResult>(DataSetName, methodCall, typeof(TResult));
    }

    /// <inheritdoc/>
    public IDataQuery<TSource> OrderBy<TKey>(Expression<Func<TSource, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var methodCall = Expression.Call(
            type: typeof(Queryable),
            methodName: nameof(Queryable.OrderBy),
            typeArguments: new[] { typeof(TSource), typeof(TKey) },
            arguments: new[] { QueryExpression, Expression.Quote(keySelector) });

        return new DataQueryBuilder<TSource>(DataSetName, methodCall);
    }

    /// <inheritdoc/>
    public IDataQuery<TSource> OrderByDescending<TKey>(Expression<Func<TSource, TKey>> keySelector)
    {
        if (keySelector == null)
            throw new ArgumentNullException(nameof(keySelector));

        var methodCall = Expression.Call(
            type: typeof(Queryable),
            methodName: nameof(Queryable.OrderByDescending),
            typeArguments: new[] { typeof(TSource), typeof(TKey) },
            arguments: new[] { QueryExpression, Expression.Quote(keySelector) });

        return new DataQueryBuilder<TSource>(DataSetName, methodCall);
    }

    /// <inheritdoc/>
    public IDataQuery<TSource> Take(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        var methodCall = Expression.Call(
            type: typeof(Queryable),
            methodName: nameof(Queryable.Take),
            typeArguments: new[] { typeof(TSource) },
            arguments: new[] { QueryExpression, Expression.Constant(count) });

        return new DataQueryBuilder<TSource>(DataSetName, methodCall);
    }

    /// <inheritdoc/>
    public IDataQuery<TSource> Skip(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");

        var methodCall = Expression.Call(
            type: typeof(Queryable),
            methodName: nameof(Queryable.Skip),
            typeArguments: new[] { typeof(TSource) },
            arguments: new[] { QueryExpression, Expression.Constant(count) });

        return new DataQueryBuilder<TSource>(DataSetName, methodCall);
    }

    /// <summary>
    /// Returns a string representation of the query for debugging purposes.
    /// </summary>
    /// <returns>A string describing the current query state.</returns>
    public override string ToString()
    {
        return $"DataQuery[{DataSetName}]: {QueryExpression}";
    }
}