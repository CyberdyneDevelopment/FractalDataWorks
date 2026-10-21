using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Describing;

/// <summary>
/// The derivers that work out the fields at a component's output nodes, one per component kind that has nodes (FDW-1397). The name
/// of an option is the name of the <c>ComponentTypes</c> option it describes.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ComponentFieldDeriverBase), typeof(IComponentFieldDeriver), typeof(ComponentFieldDerivers))]
public abstract partial class ComponentFieldDerivers : TypeCollectionBase<ComponentFieldDeriverBase, IComponentFieldDeriver>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
