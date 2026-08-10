using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Observability" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Observability")]
public sealed class ObservabilityNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservabilityNavSection"/> class.
    /// </summary>
    public ObservabilityNavSection() : base(11, "Observability", "Observability", 110) { }
}
