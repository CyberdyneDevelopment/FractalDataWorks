using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// The "Transformations" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Transformations")]
public sealed class TransformationsNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformationsNavSection"/> class.
    /// </summary>
    public TransformationsNavSection() : base("Transformations", "Transformations", 20) { }
}
