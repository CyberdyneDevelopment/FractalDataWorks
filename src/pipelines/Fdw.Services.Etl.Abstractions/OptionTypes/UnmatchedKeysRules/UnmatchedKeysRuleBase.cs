using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="UnmatchedKeysRules"/>, using the CRTP pattern.
/// </summary>
public abstract class UnmatchedKeysRuleBase : TypeOptionBase<UnmatchedKeysRuleBase>, IUnmatchedKeysRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnmatchedKeysRuleBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="displayName">The name shown to a person choosing the option.</param>
    /// <param name="description">What the option does.</param>
    /// <param name="failsLoad">Whether a batch with an unmatched key fails the destination.</param>
    protected UnmatchedKeysRuleBase(string name, string displayName, string description, bool failsLoad)
        : base(name, displayName, description, "UnmatchedKeysRules")
    {
        FailsLoad = failsLoad;
    }

    /// <inheritdoc/>
    public bool FailsLoad { get; }
}
