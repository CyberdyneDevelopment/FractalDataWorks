using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>The fire times missed while the scheduler was down are dropped; the scheduler logs how many.</summary>
[TypeOption(typeof(MissedFireRules), "Skip")]
public sealed class SkipMissedFireRule : MissedFireRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="SkipMissedFireRule"/> class.</summary>
    public SkipMissedFireRule() : base("Skip", "The fire times missed while the scheduler was down are dropped; the scheduler logs how many.", firesOnce: false)
    {
    }
}
