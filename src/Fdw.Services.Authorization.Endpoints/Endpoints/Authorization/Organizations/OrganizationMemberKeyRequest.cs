using System;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>A request about one person in an organization (FDW-1593).</summary>
public class OrganizationMemberKeyRequest : OrganizationRequest
{
    /// <summary>Gets or sets the person, from the route.</summary>
    public Guid UserId { get; set; }
}
