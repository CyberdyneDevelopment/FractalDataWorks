using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to act in another department of the company the caller acts in (FDW-1802).
/// </summary>
public class SwitchOrganizationRequest
{
    /// <summary>Gets or sets the department to act in.</summary>
    public Guid OrganizationId { get; set; }
}
