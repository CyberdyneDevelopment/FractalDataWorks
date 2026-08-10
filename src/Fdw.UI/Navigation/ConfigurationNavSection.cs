using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Configuration" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Configuration")]
public sealed class ConfigurationNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationNavSection"/> class.
    /// </summary>
    public ConfigurationNavSection() : base(9, "Configuration", "Configuration", 90) { }
}
