namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Request to switch a feature on or off for an organization (FDW-1580).
/// </summary>
public class SetOrganizationFeatureRequest : OrganizationFeaturesRequest
{
    /// <summary>Gets or sets the feature's name in the catalogue.</summary>
    public string Feature { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the feature is switched on.</summary>
    public bool Enabled { get; set; }
}
