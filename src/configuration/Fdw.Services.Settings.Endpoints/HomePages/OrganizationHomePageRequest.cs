using System;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request naming an organization whose home page is read or published (FDW-2325).
/// </summary>
public class OrganizationHomePageRequest
{
    /// <summary>Gets or sets the organization.</summary>
    public Guid OrganizationId { get; set; }
}
