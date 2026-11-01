using Fdw.Collections;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// A feature of the system that can be switched on for a tenant and then for its organizations (FDW-1580).
/// </summary>
/// <remarks>The name is what is stored; the display name and description are what the Features tab shows.</remarks>
public interface IFeature : ITypeOption<int, FeatureBase>
{
    /// <summary>Gets what the Features tab calls the feature.</summary>
    string DisplayName { get; }

    /// <summary>Gets what the feature gives the people who have it.</summary>
    string Description { get; }

    /// <summary>Gets whether this is the sentinel returned for a name the catalogue does not hold.</summary>
    bool IsEmpty { get; }
}
