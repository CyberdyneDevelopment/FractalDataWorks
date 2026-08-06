using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Registration;

/// <summary>
/// The "Developer Tools" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "DeveloperTools")]
public sealed class DeveloperToolsNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeveloperToolsNavSection"/> class.
    /// </summary>
    public DeveloperToolsNavSection() : base(10, "DeveloperTools", "Developer Tools", 100) { }
}
