using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Defines the contract for stage failure policy types.
/// Controls what happens when a Stage within a Project fails.
/// </summary>
public interface IStageFailurePolicy : ITypeOption<int, StageFailurePolicyBase>
{
}
