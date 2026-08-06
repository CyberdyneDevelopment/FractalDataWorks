using System.Collections.Generic;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Interface for join expressions (JOIN clause representation).
/// </summary>
/// <remarks>
/// Represents joins to other containers/tables.
/// Translators convert this to SQL JOINs, nested queries, etc.
/// Note: Full join support will be implemented in future phases.
/// </remarks>
public interface IJoinExpression
{
    /// <summary>
    /// Gets the target container name to join with.
    /// </summary>
    /// <value>The name of the container to join (table, collection, etc.).</value>
    string TargetContainerName { get; }

    /// <summary>
    /// Gets the join type (INNER, LEFT, RIGHT, FULL).
    /// </summary>
    /// <value>The join type as a string.</value>
    string JoinType { get; }

    /// <summary>
    /// Gets the join conditions.
    /// </summary>
    /// <value>
    /// A collection of join conditions.
    /// Example: [{ "Customers.Id", "Orders.CustomerId" }]
    /// </value>
    IReadOnlyList<(string LeftField, string RightField)> JoinConditions { get; }
}
