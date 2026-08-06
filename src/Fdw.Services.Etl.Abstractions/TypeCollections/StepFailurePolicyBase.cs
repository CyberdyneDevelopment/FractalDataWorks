using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Base class for step failure policy types using the CRTP pattern.
/// </summary>
public abstract class StepFailurePolicyBase : TypeOptionBase<int, StepFailurePolicyBase>, IStepFailurePolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StepFailurePolicyBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this policy type.</param>
    /// <param name="name">Name of the policy (must match TypeOption attribute).</param>
    protected StepFailurePolicyBase(int id, string name) : base(id, name)
    {
    }
}
