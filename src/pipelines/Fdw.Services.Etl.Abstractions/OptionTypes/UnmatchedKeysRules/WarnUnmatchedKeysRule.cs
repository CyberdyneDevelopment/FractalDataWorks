using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A batch with a key that matched no row commits; the destination counts the unmatched keys over the run and records the total and a sample.</summary>
[TypeOption(typeof(UnmatchedKeysRules), "Warn")]
public sealed class WarnUnmatchedKeysRule : UnmatchedKeysRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="WarnUnmatchedKeysRule"/> class.</summary>
    public WarnUnmatchedKeysRule()
        : base("Warn", "Warn and continue", "A batch with a key that matched no row commits; the run's records show the total of unmatched keys and a sample", failsLoad: false)
    {
    }
}
