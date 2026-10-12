using Fdw.Collections;

namespace Fdw.Services.Etl.Projects.Abstractions.TypeCollections;

/// <summary>
/// Base class for stage failure policy types using the CRTP pattern.
/// </summary>
public abstract class StageFailurePolicyBase : TypeOptionBase<StageFailurePolicyBase>, IStageFailurePolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageFailurePolicyBase"/> class.
    /// </summary>
    /// <param name="name">Name of the policy (must match TypeOption attribute).</param>
    protected StageFailurePolicyBase(string name) : base(name)
    {
    }
}
