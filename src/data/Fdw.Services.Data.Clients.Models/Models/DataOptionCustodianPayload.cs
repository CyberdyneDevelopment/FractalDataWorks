using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>A person who may be a data option rule's custodian (FDW-6339): a holder of datastores:write.</summary>
public sealed class DataOptionCustodianPayload
{
    /// <summary>Gets or sets the person's id.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the person's name.</summary>
    public string Name { get; set; } = string.Empty;
}
