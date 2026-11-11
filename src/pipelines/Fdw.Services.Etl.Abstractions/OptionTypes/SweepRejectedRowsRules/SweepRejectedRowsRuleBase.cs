using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="SweepRejectedRowsRules"/>, using the CRTP pattern.
/// </summary>
public abstract class SweepRejectedRowsRuleBase : TypeOptionBase<SweepRejectedRowsRuleBase>, ISweepRejectedRowsRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SweepRejectedRowsRuleBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person choosing the option.</param>
    /// <param name="description">What the option does.</param>
    /// <param name="sweepsAfterRejections">Whether the sweep runs although rows were rejected upstream.</param>
    protected SweepRejectedRowsRuleBase(string name, string displayName, string description, bool sweepsAfterRejections)
        : base(name, displayName, description, "SweepRejectedRowsRules")
    {
        SweepsAfterRejections = sweepsAfterRejections;
    }

    /// <inheritdoc/>
    public bool SweepsAfterRejections { get; }
}
