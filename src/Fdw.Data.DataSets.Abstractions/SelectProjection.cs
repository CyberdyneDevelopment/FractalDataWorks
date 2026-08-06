using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a SELECT projection extracted from a query.
/// </summary>
public sealed class SelectProjection
{
    /// <summary>
    /// Gets the fields being selected.
    /// </summary>
    public IReadOnlyList<ProjectedField> Fields { get; init; } = Array.Empty<ProjectedField>();

    /// <summary>
    /// Gets the original selector expression.
    /// </summary>
    public Expression OriginalExpression { get; init; } = Expression.Empty();

    /// <summary>
    /// Gets the result type of the projection.
    /// </summary>
    public Type ResultType { get; init; } = typeof(object);
}