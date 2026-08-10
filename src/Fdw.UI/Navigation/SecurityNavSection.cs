using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Security" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Security")]
public sealed class SecurityNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityNavSection"/> class.
    /// </summary>
    public SecurityNavSection() : base(8, "Security", "Security", 80) { }
}
