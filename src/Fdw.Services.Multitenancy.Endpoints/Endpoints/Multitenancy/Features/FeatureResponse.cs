namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// One feature as it stands for a tenant or an organization (FDW-1580).
/// </summary>
public class FeatureResponse
{
    /// <summary>Gets or sets the feature's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the Features tab calls the feature.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the feature gives the people who have it.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the feature is on here and at every level above.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets, when the feature is off, the level that has not switched it on: Platform or Tenant.</summary>
    public string? OffAt { get; set; }
}
