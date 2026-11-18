using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>The base of the custodian states of a data configuration rule (FDW-6346).</summary>
public abstract class DataOptionCustodianStateBase : TypeOptionBase<DataOptionCustodianStateBase>, IDataOptionCustodianState
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionCustodianStateBase"/> class.</summary>
    /// <param name="name">The state's name.</param>
    /// <param name="needsAttention">Whether a rule in this state needs someone's attention.</param>
    protected DataOptionCustodianStateBase(string name, bool needsAttention)
        : base(name)
    {
        NeedsAttention = needsAttention;
    }

    /// <inheritdoc />
    public bool NeedsAttention { get; }
}
