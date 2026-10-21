using System;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request naming an organization whose retention is read or run (FDW-1575).
/// </summary>
public class OrganizationRetentionRequest
{
    /// <summary>Gets or sets the organization.</summary>
    public Guid OrganizationId { get; set; }
}
