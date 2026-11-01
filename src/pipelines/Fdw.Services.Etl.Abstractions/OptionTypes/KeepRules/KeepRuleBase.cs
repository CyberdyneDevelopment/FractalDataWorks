using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="KeepRules"/>, using the CRTP pattern.
/// </summary>
public abstract class KeepRuleBase : TypeOptionBase<KeepRuleBase>, IKeepRule
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeepRuleBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="keepsLargest">Whether the rule keeps the largest order value (and, of equals, the latest row) rather than the smallest (and the earliest).</param>
    protected KeepRuleBase(string name, bool keepsLargest) : base(name, "KeepRules")
    {
        KeepsLargest = keepsLargest;
    }

    /// <summary>Gets whether the rule keeps the largest order value rather than the smallest.</summary>
    public bool KeepsLargest { get; }

    /// <inheritdoc/>
    public bool Replaces(int candidateToKeptComparison) => KeepsLargest ? candidateToKeptComparison >= 0 : candidateToKeptComparison < 0;
}
