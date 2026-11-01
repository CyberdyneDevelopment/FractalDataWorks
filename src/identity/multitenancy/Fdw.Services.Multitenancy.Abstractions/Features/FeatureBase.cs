using Fdw.Collections;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Base for a feature in the feature catalogue (FDW-1580). A package adds its own feature by declaring a TypeOption of
/// <see cref="Features"/> that derives from this; referencing the package is what puts the feature in the catalogue.
/// </summary>
public abstract class FeatureBase : TypeOptionBase<FeatureBase>, IFeature
{
    /// <summary>Initializes the sentinel returned for a name the catalogue does not hold.</summary>
    protected FeatureBase()
        : base(0, "NotFound", "TypeOptions:NotFound", "Not Found", "Unknown feature", null)
    {
    }

    /// <summary>Initializes a feature.</summary>
    /// <param name="name">The feature's name, which is what is stored.</param>
    /// <param name="displayName">What the Features tab calls it.</param>
    /// <param name="description">What the feature gives the people who have it.</param>
    protected FeatureBase(string name, string displayName, string description)
        : base(name, $"TypeOptions:{name}", displayName, description, null)
    {
    }

    /// <inheritdoc />
    /// <remarks>The sentinel is the one option whose identifier is zero; every real option's is derived from its type.</remarks>
    public bool IsEmpty => Id == 0;
}
