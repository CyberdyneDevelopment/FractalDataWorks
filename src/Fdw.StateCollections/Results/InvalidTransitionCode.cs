using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.StateCollections.Results;

/// <summary>The requested target state is not in the current state's <c>CanProgressTo</c>.</summary>
[TypeOption(typeof(StateMachineResultCodes), "InvalidTransition", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidTransitionCode : StateMachineResultCodeBase
{
    /// <summary>Initializes a new instance.</summary>
    public InvalidTransitionCode()
        : base(40000, "InvalidTransition",
            ResultSeverities.ByName("Error"),
            "Invalid transition: state '{Current}' cannot progress to '{Target}'",
            isRetryable: false)
    {
    }
}
