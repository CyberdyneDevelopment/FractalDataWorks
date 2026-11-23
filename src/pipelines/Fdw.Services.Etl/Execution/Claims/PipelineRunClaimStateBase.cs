using Fdw.Collections;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>Base class for the states of a run's claim.</summary>
public abstract class PipelineRunClaimStateBase : TypeOptionBase<PipelineRunClaimStateBase>, IPipelineRunClaimState
{
    /// <summary>Initializes a new instance of the <see cref="PipelineRunClaimStateBase"/> class.</summary>
    /// <param name="name">The option name; it is also the <c>State</c> the claim procedures store and answer.</param>
    protected PipelineRunClaimStateBase(string name)
        : base(name)
    {
    }
}
