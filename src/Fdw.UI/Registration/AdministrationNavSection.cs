using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Registration;

/// <summary>
/// The "Administration" sidebar section.
/// </summary>
/// <remarks>
/// Why 120 rather than the 50 it previously declared: "Administration" and "Quality" both claimed 50,
/// so their relative placement was undefined. Administration sorts last; every other section keeps the
/// order it already had.
/// </remarks>
[TypeOption(typeof(NavSections), "Administration")]
public sealed class AdministrationNavSection : NavSectionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdministrationNavSection"/> class.
    /// </summary>
    public AdministrationNavSection() : base(12, "Administration", "Administration", 120) { }
}
