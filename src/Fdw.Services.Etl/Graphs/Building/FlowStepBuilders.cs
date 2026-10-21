using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// The builders of the engine's steps (FDW-1403): one option for each action or loop kind of <c>ComponentTypes</c>, named for it.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(FlowStepBuilderBase), typeof(IFlowStepBuilder), typeof(FlowStepBuilders))]
public abstract partial class FlowStepBuilders : TypeCollectionBase<FlowStepBuilderBase, IFlowStepBuilder>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
