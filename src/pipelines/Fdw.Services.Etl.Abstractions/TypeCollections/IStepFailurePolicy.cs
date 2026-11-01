using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Defines the contract for step failure policy types.
/// Controls what happens when a Pipeline within a Step fails.
/// </summary>
public interface IStepFailurePolicy : ITypeOption<int, StepFailurePolicyBase>
{
}
