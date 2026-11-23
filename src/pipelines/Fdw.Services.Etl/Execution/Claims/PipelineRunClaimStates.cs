using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>The states of a run's claim: Claimed and Waiting (FDW-6629). A state the procedures answer that matches none gets <c>NotFound</c> and the claim is refused.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PipelineRunClaimStateBase), typeof(IPipelineRunClaimState), typeof(PipelineRunClaimStates))]
public abstract partial class PipelineRunClaimStates : TypeCollectionBase<PipelineRunClaimStateBase, IPipelineRunClaimState>
{
}
