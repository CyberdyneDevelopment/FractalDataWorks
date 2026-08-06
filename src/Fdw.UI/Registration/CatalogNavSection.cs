using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Registration;

/// <summary>
/// The "Catalog" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Catalog")]
public sealed class CatalogNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CatalogNavSection"/> class.
    /// </summary>
    public CatalogNavSection() : base(6, "Catalog", "Catalog", 60) { }
}
