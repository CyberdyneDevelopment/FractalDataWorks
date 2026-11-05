using Fdw.Collections;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="MissedFireRules"/>, using the CRTP pattern.
/// </summary>
public abstract class MissedFireRuleBase : TypeOptionBase<MissedFireRuleBase>, IMissedFireRule
{
    /// <summary>Initializes a new instance of the <see cref="MissedFireRuleBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the scheduler does, as the console shows it beside the rule.</param>
    /// <param name="firesOnce">Whether the scheduler runs the schedule once at start for the fire times it missed.</param>
    protected MissedFireRuleBase(string name, string description, bool firesOnce) : base(name, name, description, "MissedFireRules")
    {
        FiresOnce = firesOnce;
    }

    /// <inheritdoc/>
    public bool FiresOnce { get; }
}
