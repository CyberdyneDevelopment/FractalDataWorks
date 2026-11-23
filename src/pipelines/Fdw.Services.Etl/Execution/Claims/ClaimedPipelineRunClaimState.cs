using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>The run holds every key it asked for and may start.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PipelineRunClaimStates), "Claimed")]
public sealed class ClaimedPipelineRunClaimState : PipelineRunClaimStateBase
{
    /// <summary>Initializes a new instance of the <see cref="ClaimedPipelineRunClaimState"/> class.</summary>
    public ClaimedPipelineRunClaimState()
        : base("Claimed")
    {
    }
}
