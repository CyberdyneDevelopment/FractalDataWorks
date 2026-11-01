namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// The features of the catalogue as they stand for a tenant or an organization (FDW-1580).
/// </summary>
public class FeaturesResponse
{
    /// <summary>Gets or sets the features.</summary>
    public System.Collections.Generic.IList<FeatureResponse> Features { get; set; } = [];
}
