using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The owner a data option rule is added or changed with (FDW-6339).</summary>
public sealed class DataOptionOwnerBody
{
    /// <summary>Gets or sets the kind of owner: Organization or Workspace (a DataOptionOwnerKinds name).</summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets the owner's id.</summary>
    public Guid? Id { get; set; }
}
