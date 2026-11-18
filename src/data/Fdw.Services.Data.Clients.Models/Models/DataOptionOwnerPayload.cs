using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The organization or workspace a data option rule belongs to (FDW-6339).</summary>
public sealed class DataOptionOwnerPayload
{
    /// <summary>Gets or sets the kind of owner: Organization or Workspace (a DataOptionOwnerKinds name).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the owner's id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owner's name.</summary>
    public string Name { get; set; } = string.Empty;
}
