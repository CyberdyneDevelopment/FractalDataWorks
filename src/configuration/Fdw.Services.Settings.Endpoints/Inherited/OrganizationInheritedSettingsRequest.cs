using System;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Request naming an organization whose settings are read (FDW-1579).
/// </summary>
public class OrganizationInheritedSettingsRequest
{
    /// <summary>Gets or sets the organization.</summary>
    public Guid OrganizationId { get; set; }
}
