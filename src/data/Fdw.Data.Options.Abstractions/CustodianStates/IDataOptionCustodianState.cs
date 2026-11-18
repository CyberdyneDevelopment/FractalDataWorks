using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>A state of a data configuration rule's custodian: holds the permission or no longer does (FDW-6346).</summary>
public interface IDataOptionCustodianState : ITypeOption<int, DataOptionCustodianStateBase>
{
    /// <summary>Gets the name a console shows for this option (FDW-6342); every option is a type option, which states one.</summary>
    string DisplayName { get; }

    /// <summary>Gets whether a rule in this state needs someone's attention, so a console shows it differently without naming the state (FDW-6342).</summary>
    bool NeedsAttention { get; }
}
