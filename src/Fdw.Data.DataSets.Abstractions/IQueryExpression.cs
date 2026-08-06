using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fdw.Commands.Abstractions;
using Fdw.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a structured query expression that can be analyzed and translated
/// to different backend query languages (SQL, REST, file operations, etc.).
/// </summary>
public interface IQueryExpression
{
    /// <summary>
    /// Gets the original LINQ expression tree.
    /// Contains the raw expression as captured from LINQ methods.
    /// </summary>
    Expression OriginalExpression { get; }

    /// <summary>
    /// Gets the dataset being queried.
    /// </summary>
    IDataSetType DataSet { get; }

    /// <summary>
    /// Gets the Where clauses extracted from the expression.
    /// Each condition can be analyzed separately for optimization.
    /// </summary>
    IReadOnlyList<WhereClause> WhereConditions { get; }

    /// <summary>
    /// Gets the Select projection if present.
    /// Null if no Select clause (returns full records).
    /// </summary>
    SelectProjection? SelectProjection { get; }

    /// <summary>
    /// Gets the OrderBy clauses if present.
    /// </summary>
    IReadOnlyList<OrderByClause> OrderByConditions { get; }

    /// <summary>
    /// Gets the Skip count if present (for pagination).
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Gets the Take count if present (for pagination/limiting).
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Gets the result type after all projections.
    /// May be anonymous type for Select projections.
    /// </summary>
    Type ResultType { get; }
}

