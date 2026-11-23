using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions;

/// <summary>A run found half-done ends, closed Interrupted, and never runs again.</summary>
[TypeOption(typeof(HalfDoneRules), "End")]
public sealed class EndHalfDoneRule : HalfDoneRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="EndHalfDoneRule"/> class.</summary>
    public EndHalfDoneRule()
        : base("End", "A run found half-done after a restart ends: its record is closed Interrupted and it never runs again.")
    {
    }
}
