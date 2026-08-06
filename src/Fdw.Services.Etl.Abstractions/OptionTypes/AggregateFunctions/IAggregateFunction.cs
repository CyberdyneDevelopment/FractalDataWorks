using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Interface for aggregate function type options consumed by Aggregate transforms.
/// </summary>
public interface IAggregateFunction : ITypeOption<int, IAggregateFunction>
{
    /// <summary>
    /// Reduces the given values (already filtered to non-null) according to this function's semantics.
    /// </summary>
    /// <param name="values">The non-null, non-empty values collected for one group's source field.</param>
    /// <returns>The reduced value.</returns>
    object? Apply(IReadOnlyList<object?> values);
}
