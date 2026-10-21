namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The feature catalogue (FDW-1580).
/// </summary>
public class FeatureCatalogueResponse
{
    /// <summary>Gets or sets every feature the system knows.</summary>
    public System.Collections.Generic.IList<FeatureCatalogueEntry> Features { get; set; } = [];
}
