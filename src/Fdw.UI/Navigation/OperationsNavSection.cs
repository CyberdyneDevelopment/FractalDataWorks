using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Operations" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Operations")]
public sealed class OperationsNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OperationsNavSection"/> class.
    /// </summary>
    public OperationsNavSection() : base(7, "Operations", "Operations", 70) { }
}
