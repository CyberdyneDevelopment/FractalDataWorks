using Fdw.Collections.Attributes;

namespace Fdw.Data.Options;

/// <summary>The custodian holds datastores:write (FDW-6346).</summary>
[TypeOption(typeof(DataOptionCustodianStates), "Holder")]
public sealed class HolderCustodianState : DataOptionCustodianStateBase
{
    /// <summary>Initializes a new instance of the <see cref="HolderCustodianState"/> class.</summary>
    public HolderCustodianState()
        : base("Holder", needsAttention: false)
    {
    }
}
