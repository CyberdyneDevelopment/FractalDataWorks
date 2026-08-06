using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Web.Abstractions;

/// <summary>
/// Collection of UI component types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ComponentTypeBase), typeof(IComponentType), typeof(ComponentTypes))]
public abstract partial class ComponentTypes : TypeCollectionBase<ComponentTypeBase, IComponentType>
{
}

