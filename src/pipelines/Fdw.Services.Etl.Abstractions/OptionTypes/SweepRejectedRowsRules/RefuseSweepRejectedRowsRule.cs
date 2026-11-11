using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>The destination fails instead of sweeping when any row was rejected upstream: no target row is flagged for a key the source still has.</summary>
[TypeOption(typeof(SweepRejectedRowsRules), "Refuse")]
public sealed class RefuseSweepRejectedRowsRule : SweepRejectedRowsRuleBase
{
    /// <summary>Initializes a new instance of the <see cref="RefuseSweepRejectedRowsRule"/> class.</summary>
    public RefuseSweepRejectedRowsRule()
        : base("Refuse", "Refuse the sweep", "When a row was rejected upstream, the destination fails and flags nothing: a rejected row's key would look deleted", sweepsAfterRejections: false)
    {
    }
}
