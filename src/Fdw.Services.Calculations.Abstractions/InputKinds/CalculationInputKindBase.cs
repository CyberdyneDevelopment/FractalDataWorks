using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Base class for calculation input kinds (CRTP pattern).
/// </summary>
public abstract class CalculationInputKindBase : TypeOptionBase<CalculationInputKindBase>, ICalculationInputKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculationInputKindBase"/> class.
    /// </summary>
    /// <param name="name">The name of this input kind.</param>
    protected CalculationInputKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IReadOnlyList<CalculationSetting> Settings { get; }
}
