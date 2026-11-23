using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>The run waits behind another run that holds, or asked earlier for, one of its keys.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PipelineRunClaimStates), "Waiting")]
public sealed class WaitingPipelineRunClaimState : PipelineRunClaimStateBase
{
    /// <summary>Initializes a new instance of the <see cref="WaitingPipelineRunClaimState"/> class.</summary>
    public WaitingPipelineRunClaimState()
        : base("Waiting")
    {
    }
}
