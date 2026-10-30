using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="OverflowBehaviors"/>, using the CRTP pattern.
/// </summary>
public abstract class OverflowBehaviorBase : TypeOptionBase<OverflowBehaviorBase>, IOverflowBehavior
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverflowBehaviorBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What happens to the run, as the console shows it beside the option.</param>
    /// <param name="refusesWhenFull">Whether the run is refused; when false it waits for its turn.</param>
    /// <param name="holdsWhenFull">Whether the run is held until a set time before it waits for its turn (FDW-3129).</param>
    protected OverflowBehaviorBase(string name, string description, bool refusesWhenFull, bool holdsWhenFull = false) : base(name, name, description, "OverflowBehaviors")
    {
        RefusesWhenFull = refusesWhenFull;
        HoldsWhenFull = holdsWhenFull;
    }

    /// <inheritdoc/>
    public bool RefusesWhenFull { get; }

    /// <inheritdoc/>
    public bool HoldsWhenFull { get; }
}
