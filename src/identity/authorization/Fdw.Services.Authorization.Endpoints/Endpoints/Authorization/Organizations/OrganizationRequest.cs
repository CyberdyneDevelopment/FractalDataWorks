using System;

namespace Fdw.Services.Authorization.Endpoints;

/// <summary>A request about one organization (FDW-1593).</summary>
public class OrganizationRequest
{
    /// <summary>Gets or sets the organization, from the route.</summary>
    public Guid OrganizationId { get; set; }
}
