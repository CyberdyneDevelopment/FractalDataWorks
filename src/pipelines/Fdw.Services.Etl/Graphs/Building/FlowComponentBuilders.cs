using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// The builders that turn a stored component into a flow component, one per component kind (FDW-1396). The name of an option is
/// the name of the <c>ComponentTypes</c> option it builds.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(FlowComponentBuilderBase), typeof(IFlowComponentBuilder), typeof(FlowComponentBuilders))]
public abstract partial class FlowComponentBuilders : TypeCollectionBase<FlowComponentBuilderBase, IFlowComponentBuilder>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
