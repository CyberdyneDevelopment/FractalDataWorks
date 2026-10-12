using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Developer Tools" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "DeveloperTools")]
public sealed class DeveloperToolsNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeveloperToolsNavSection"/> class.
    /// </summary>
    public DeveloperToolsNavSection() : base("DeveloperTools", "Developer Tools", 100) { }
}
