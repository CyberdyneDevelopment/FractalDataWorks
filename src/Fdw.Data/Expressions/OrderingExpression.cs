using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data;

/// <summary>
/// Implementation of IOrderingExpression for ORDER BY clause representation.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class OrderingExpression : IOrderingExpression
{
    /// <summary>
    /// Gets or sets the ordered fields.
    /// Order in the list determines sort precedence.
    /// </summary>
    public required IReadOnlyList<IOrderedField> OrderedFields { get; init; }
}
