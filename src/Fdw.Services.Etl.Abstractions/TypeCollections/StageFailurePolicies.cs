using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Collection of stage failure policy types.
/// Controls behavior when a Stage within a Project fails.
/// HaltProject is stricter than ContinueProject.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(StageFailurePolicyBase), typeof(IStageFailurePolicy), typeof(StageFailurePolicies))]
public abstract partial class StageFailurePolicies : TypeCollectionBase<StageFailurePolicyBase, IStageFailurePolicy>
{
}
