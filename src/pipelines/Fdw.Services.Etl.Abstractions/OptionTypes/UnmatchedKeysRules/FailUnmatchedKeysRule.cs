using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>A batch with a key that matched no row fails the destination: its unit of work rolls back and the run fails, naming the count and a sample of the keys.</summary>
[TypeOption(typeof(UnmatchedKeysRules), "Fail")]
public sealed class FailUnmatchedKeysRule : UnmatchedKeysRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="FailUnmatchedKeysRule"/> class.</summary>
    public FailUnmatchedKeysRule()
        : base("Fail", "Fail the load", "A batch with a key that matched no row rolls back and the run fails, naming the count and a sample of the keys", failsLoad: true)
    {
    }
}
