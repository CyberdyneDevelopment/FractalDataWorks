using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Composition;

/// <summary>
/// TypeCollection of composable components available to arrange into a view.
/// </summary>
/// <remarks>
/// The runtime catalogue a palette enumerates and a layout host resolves against. Because it is a
/// TypeCollection, an app publishes its own components with <c>[TypeOption]</c> in its own assembly
/// and they appear alongside FDW's, with no registration list to maintain and nothing to edit here.
/// </remarks>
[TypeCollection(typeof(ComponentDescriptorBase), typeof(IComponentDescriptor), typeof(ComponentCatalog))]
public sealed partial class ComponentCatalog : TypeCollectionBase<ComponentDescriptorBase, IComponentDescriptor>
{
}
