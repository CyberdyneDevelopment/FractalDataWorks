using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IOrderingExpression for ORDER BY clause representation.
/// </summary>
/// <remarks>
/// Two orderings with the same fields in the same order and directions are equal and hash alike. The
/// result cache keys a query on its ordering's hash, so an ordering that hashed by reference gave every
/// request its own key -- a cache that never hit -- and, on a hash collision between two live
/// instances, served one ordering's rows for another's.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class OrderingExpression : IOrderingExpression, IEquatable<OrderingExpression>
{
    /// <summary>
    /// Gets or sets the ordered fields.
    /// Order in the list determines sort precedence.
    /// </summary>
    public required IReadOnlyList<IOrderedField> OrderedFields { get; init; }

    /// <inheritdoc/>
    public bool Equals(OrderingExpression? other) =>
        other is not null && OrderedFields.SequenceEqual(other.OrderedFields);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is OrderingExpression other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var field in OrderedFields)
            hash.Add(field);
        return hash.ToHashCode();
    }
}
