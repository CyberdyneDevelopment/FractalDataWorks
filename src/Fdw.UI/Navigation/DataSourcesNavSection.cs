using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Data Sources" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "DataSources")]
public sealed class DataSourcesNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSourcesNavSection"/> class.
    /// </summary>
    public DataSourcesNavSection() : base(1, "DataSources", "Data Sources", 10) { }
}
