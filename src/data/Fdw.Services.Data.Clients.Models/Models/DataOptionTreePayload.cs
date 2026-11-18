using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>Every level, from the DataConfigurationRules root down, to any depth (FDW-6339), and the names a rule chooses from.</summary>
public sealed class DataOptionTreePayload
{
    /// <summary>Gets or sets the DataConfigurationRules root node.</summary>
    public required DataOptionTreeNodePayload Root { get; set; }


    /// <summary>Gets or sets the kinds of value (the DataOptionValueKinds collection), each with the member of a value that holds its content.</summary>
    public List<DataOptionValueKindPayload> ValueKinds { get; set; } = [];

    /// <summary>Gets or sets the kinds of owner (the DataOptionOwnerKinds collection).</summary>
    public List<DataOptionNamedPayload> OwnerKinds { get; set; } = [];

    /// <summary>Gets or sets the states of a rule's custodian (the DataOptionCustodianStates collection): Holder, or NoLongerHolder when they lost datastores:write, each stating whether a rule in it needs attention.</summary>
    public List<DataOptionCustodianStatePayload> CustodianStates { get; set; } = [];
}
