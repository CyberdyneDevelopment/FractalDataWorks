using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Defines a join relationship between two containers.
/// Used by CompoundQueryCommand to specify JOIN clauses.
/// </summary>
public interface IJoinDefinition
{
    /// <summary>
    /// Gets the name of the container to join with.
    /// </summary>
    /// <value>The container name (e.g., "Orders", "OrderDetails").</value>
    string ContainerName { get; }

    /// <summary>
    /// Gets the alias for this joined container (optional).
    /// </summary>
    /// <value>The alias used in queries (e.g., "o" for Orders, "od" for OrderDetails).</value>
    string? Alias { get; }

    /// <summary>
    /// Gets the type of join operation.
    /// </summary>
    /// <value>The join type instance from JoinTypes collection.</value>
    IJoinType JoinType { get; }

    /// <summary>
    /// Gets the join conditions (ON clause).
    /// </summary>
    /// <value>
    /// Collection of field pairs that form the join condition.
    /// Format: (LeftField, RightField) tuples.
    /// Examples:
    /// - ("Customers.Id", "Orders.CustomerId")
    /// - ("Orders.Id", "OrderDetails.OrderId")
    /// Multiple conditions are combined with AND.
    /// </value>
    IReadOnlyList<(string LeftField, string RightField)> Conditions { get; }
}
