using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Asks to join a dataverse.</summary>
public class RequestDataverseMembershipRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the membership would be for: User or Role.</summary>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the user or role the membership would be for.</summary>
    public Guid SubjectId { get; set; }

    /// <summary>Gets or sets the role being asked for.</summary>
    public string RequestedRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the stated reason for the request.</summary>
    public string? Justification { get; set; }
}
