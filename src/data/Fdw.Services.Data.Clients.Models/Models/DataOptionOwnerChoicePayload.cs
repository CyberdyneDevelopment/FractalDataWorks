using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>An organization or workspace the caller may own a data option rule in (FDW-6339).</summary>
public sealed class DataOptionOwnerChoicePayload
{
    /// <summary>Gets or sets the owner's id, as a rule's <c>owner.id</c> states it.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owner's name.</summary>
    public string Name { get; set; } = string.Empty;
}
