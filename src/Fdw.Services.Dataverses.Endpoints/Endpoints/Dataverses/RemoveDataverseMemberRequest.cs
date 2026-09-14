using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Removes a member from a dataverse.</summary>
public class RemoveDataverseMemberRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the membership being removed.</summary>
    public Guid MemberId { get; set; }
}
