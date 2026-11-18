namespace Fdw.Services.Data.Clients.Models;

/// <summary>A state of a data option rule's custodian (FDW-6342): its name, its display name, and whether a rule in it needs attention, so a console shows it differently without naming it.</summary>
public sealed class DataOptionCustodianStatePayload
{
    /// <summary>Gets or sets the state's name (a DataOptionCustodianStates name), as a rule's <c>custodianState</c> states it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the state's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a rule in this state needs someone's attention: the custodian no longer holds the permission.</summary>
    public bool NeedsAttention { get; set; }
}
