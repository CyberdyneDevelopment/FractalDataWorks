using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Base class for scalar value types (CRTP pattern).
/// </summary>
public abstract class ScalarValueTypeBase : TypeOptionBase<int, ScalarValueTypeBase>, IScalarValueType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScalarValueTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this scalar value type.</param>
    /// <param name="name">The name of this scalar value type.</param>
    protected ScalarValueTypeBase(int id, string name)
        : base(id, name)
    {
    }
}
