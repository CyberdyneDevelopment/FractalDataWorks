using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Registration;

/// <summary>
/// The "Scheduling" sidebar section.
/// </summary>
[TypeOption(typeof(NavSections), "Scheduling")]
public sealed class SchedulingNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulingNavSection"/> class.
    /// </summary>
    public SchedulingNavSection() : base(4, "Scheduling", "Scheduling", 40) { }
}
