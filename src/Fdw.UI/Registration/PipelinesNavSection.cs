using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Registration;

/// <summary>
/// The "Pipelines" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Pipelines")]
public sealed class PipelinesNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PipelinesNavSection"/> class.
    /// </summary>
    public PipelinesNavSection() : base(3, "Pipelines", "Pipelines", 30) { }
}
