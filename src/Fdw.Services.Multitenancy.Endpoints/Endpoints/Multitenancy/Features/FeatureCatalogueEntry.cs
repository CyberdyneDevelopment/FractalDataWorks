namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// One feature of the catalogue (FDW-1580).
/// </summary>
public class FeatureCatalogueEntry
{
    /// <summary>Gets or sets the feature's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the Features tab calls the feature.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the feature gives the people who have it.</summary>
    public string Description { get; set; } = string.Empty;
}
