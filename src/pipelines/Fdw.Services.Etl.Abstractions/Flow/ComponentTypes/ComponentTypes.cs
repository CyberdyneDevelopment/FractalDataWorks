using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The kinds of component a pipeline graph is built from (FDW-1395). The console draws its palette, ports and forms from this
/// collection through the type-collection listing, so a new kind appears without a console release.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ComponentTypeBase), typeof(IComponentType), typeof(ComponentTypes))]
public abstract partial class ComponentTypes : TypeCollectionBase<ComponentTypeBase, IComponentType>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
