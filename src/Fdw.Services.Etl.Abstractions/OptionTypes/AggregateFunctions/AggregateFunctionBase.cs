using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for aggregate function type options using the CRTP pattern.
/// </summary>
public abstract class AggregateFunctionBase : TypeOptionBase<int, AggregateFunctionBase>, IAggregateFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateFunctionBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The function name (e.g., "Sum", "Count").</param>
    protected AggregateFunctionBase(int id, string name) : base(id, name, "AggregateFunctions")
    {
    }

    /// <inheritdoc/>
    public abstract object? Apply(IReadOnlyList<object?> values);
}
