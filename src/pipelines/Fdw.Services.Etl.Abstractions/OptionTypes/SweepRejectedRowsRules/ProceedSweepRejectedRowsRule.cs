using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The sweep runs although rows were rejected upstream: the target rows of rejected keys are flagged as gone from the source.</summary>
[TypeOption(typeof(SweepRejectedRowsRules), "Proceed")]
public sealed class ProceedSweepRejectedRowsRule : SweepRejectedRowsRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="ProceedSweepRejectedRowsRule"/> class.</summary>
    public ProceedSweepRejectedRowsRule()
        : base("Proceed", "Sweep anyway", "The sweep runs although rows were rejected upstream; the target rows of rejected keys are flagged as gone from the source", sweepsAfterRejections: true)
    {
    }
}
