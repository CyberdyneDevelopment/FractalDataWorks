using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Navigation;

/// <summary>
/// TypeCollection for sidebar sections. The source generator creates a static property per section
/// declared with <c>[TypeOption]</c>.
/// </summary>
/// <remarks>
/// A downstream package contributes a section by declaring its own <c>[TypeOption(typeof(NavSections), …)]</c>
/// in its own assembly, exactly as it contributes a page to <see cref="PageTypes"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(NavSectionBase), typeof(INavSection), typeof(NavSections))]
public abstract partial class NavSections : TypeCollectionBase<NavSectionBase, INavSection>
{
}
