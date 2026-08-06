using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Collection of step failure policy types.
/// Controls behavior when a Pipeline within a Step fails.
/// HaltStage is stricter than ContinueStage.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(StepFailurePolicyBase), typeof(IStepFailurePolicy), typeof(StepFailurePolicies))]
public abstract partial class StepFailurePolicies : TypeCollectionBase<StepFailurePolicyBase, IStepFailurePolicy>
{
}
