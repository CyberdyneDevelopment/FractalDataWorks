using Fdw.Collections.Attributes;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>When the scheduler starts it runs the schedule once for the fire times missed while it was down, however many there were.</summary>
[TypeOption(typeof(MissedFireRules), "FireOnce")]
public sealed class FireOnceMissedFireRule : MissedFireRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="FireOnceMissedFireRule"/> class.</summary>
    public FireOnceMissedFireRule() : base("FireOnce", "When the scheduler starts it runs the schedule once for the fire times missed while it was down, however many there were.", firesOnce: true)
    {
    }
}
