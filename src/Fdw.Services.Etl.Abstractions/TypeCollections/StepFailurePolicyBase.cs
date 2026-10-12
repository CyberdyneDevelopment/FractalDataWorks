using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Base class for step failure policy types using the CRTP pattern.
/// </summary>
public abstract class StepFailurePolicyBase : TypeOptionBase<StepFailurePolicyBase>, IStepFailurePolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StepFailurePolicyBase"/> class.
    /// </summary>
    /// <param name="name">Name of the policy (must match TypeOption attribute).</param>
    protected StepFailurePolicyBase(string name) : base(name)
    {
    }
}
