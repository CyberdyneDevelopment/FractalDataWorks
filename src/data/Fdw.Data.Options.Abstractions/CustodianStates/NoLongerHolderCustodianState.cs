using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>The custodian no longer holds datastores:write; the rule still names them and can be given another custodian (FDW-6346).</summary>
[TypeOption(typeof(DataOptionCustodianStates), "NoLongerHolder")]
public sealed class NoLongerHolderCustodianState : DataOptionCustodianStateBase
{
    /// <summary>Initializes a new instance of the <see cref="NoLongerHolderCustodianState"/> class.</summary>
    public NoLongerHolderCustodianState()
        : base("NoLongerHolder", needsAttention: true)
    {
    }
}
