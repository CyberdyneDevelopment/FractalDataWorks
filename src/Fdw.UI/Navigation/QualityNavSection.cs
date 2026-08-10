using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Quality" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Quality")]
public sealed class QualityNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualityNavSection"/> class.
    /// </summary>
    public QualityNavSection() : base(5, "Quality", "Quality", 50) { }
}
