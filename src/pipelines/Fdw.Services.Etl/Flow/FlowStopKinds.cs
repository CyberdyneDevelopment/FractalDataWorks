using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Flow;

/// <summary>Why a level's flow can be stopped (FDW-4150, was the FlowStopKind enum): not at all, cancelled, or by the host stopping.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(FlowStopKindBase), typeof(IFlowStopKind), typeof(FlowStopKinds))]
public abstract partial class FlowStopKinds : TypeCollectionBase<FlowStopKindBase, IFlowStopKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
