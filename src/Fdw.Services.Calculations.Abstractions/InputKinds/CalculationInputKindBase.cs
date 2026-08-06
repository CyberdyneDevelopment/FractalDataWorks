using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Base class for calculation input kinds (CRTP pattern).
/// </summary>
public abstract class CalculationInputKindBase : TypeOptionBase<int, CalculationInputKindBase>, ICalculationInputKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationInputKindBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this input kind.</param>
    /// <param name="name">The name of this input kind.</param>
    protected CalculationInputKindBase(int id, string name)
        : base(id, name)
    {
    }
}
