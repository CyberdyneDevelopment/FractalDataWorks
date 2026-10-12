using System;
using System.Linq.Expressions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a query against a dataset with LINQ expression support.
/// This interface captures query expressions for translation to different backends.
/// </summary>
/// <remarks>
/// IDataQuery serves as the bridge between user LINQ expressions and backend-specific
/// query languages (SQL, REST parameters, file filters, etc.). The Expression property
/// contains the complete expression tree that can be visited and translated by
/// connection-specific query translators.
/// </remarks>
public interface IDataQuery
{
    /// <summary>
    /// Gets the LINQ expression tree representing this query.
    /// </summary>
    /// <value>
    /// The complete expression tree captured from LINQ method calls.
    /// This includes Where, Select, OrderBy, GroupBy, and other LINQ operators.
    /// </value>
    Expression QueryExpression { get; }

    /// <summary>
    /// Gets the name of the dataset this query targets.
    /// </summary>
    /// <value>The dataset name for lookup in the DataSetProvider.</value>
    string DataSetName { get; }

    /// <summary>
    /// Gets the expected result type of this query.
    /// </summary>
    /// <value>The type that results should be mapped to after execution.</value>
    Type ResultType { get; }

    /// <summary>
    /// Gets the source record type of the dataset.
    /// </summary>
    /// <value>The underlying record type of the dataset being queried.</value>
    Type SourceType { get; }
}

/// <summary>
/// Generic version of IDataQuery with strong typing for the source dataset.
/// </summary>
/// <typeparam name="TSource">The type of records in the source dataset.</typeparam>
/// <remarks>
/// This generic interface provides compile-time type safety for dataset queries
/// while maintaining the ability to work with expression trees for translation.
/// </remarks>
public interface IDataQuery<TSource> : IDataQuery
{
    /// <summary>
    /// Creates a new query with an additional Where clause.
    /// </summary>
    /// <param name="predicate">The filter condition to apply.</param>
    /// <returns>A new query instance with the combined conditions.</returns>
    IDataQuery<TSource> Where(Expression<Func<TSource, bool>> predicate);

    /// <summary>
    /// Creates a new query that selects specific fields or transforms records.
    /// </summary>
    /// <typeparam name="TResult">The type of the selected/transformed result.</typeparam>
    /// <param name="selector">The selection or transformation expression.</param>
    /// <returns>A new query instance that will return TResult records.</returns>
    IDataQuery<TResult> Select<TResult>(Expression<Func<TSource, TResult>> selector);

    /// <summary>
    /// Creates a new query with ordering applied.
    /// </summary>
    /// <typeparam name="TKey">The type of the ordering key.</typeparam>
    /// <param name="keySelector">The expression that selects the ordering key.</param>
    /// <returns>A new query instance with ordering applied.</returns>
    IDataQuery<TSource> OrderBy<TKey>(Expression<Func<TSource, TKey>> keySelector);

    /// <summary>
    /// Creates a new query with descending ordering applied.
    /// </summary>
    /// <typeparam name="TKey">The type of the ordering key.</typeparam>
    /// <param name="keySelector">The expression that selects the ordering key.</param>
    /// <returns>A new query instance with descending ordering applied.</returns>
    IDataQuery<TSource> OrderByDescending<TKey>(Expression<Func<TSource, TKey>> keySelector);

    /// <summary>
    /// Creates a new query that limits the number of results.
    /// </summary>
    /// <param name="count">The maximum number of records to return.</param>
    /// <returns>A new query instance with the limit applied.</returns>
    IDataQuery<TSource> Take(int count);

    /// <summary>
    /// Creates a new query that skips a specified number of results.
    /// </summary>
    /// <param name="count">The number of records to skip.</param>
    /// <returns>A new query instance with the skip applied.</returns>
    IDataQuery<TSource> Skip(int count);
}