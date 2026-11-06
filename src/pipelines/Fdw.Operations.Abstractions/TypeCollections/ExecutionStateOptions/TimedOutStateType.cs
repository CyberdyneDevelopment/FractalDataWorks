using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// The state of an execution that ran past its own time limit (FDW-4278). It is not Cancelled (nobody cancelled it), not Interrupted (the host did not stop) and not
/// Failed (the work did not fail): the run exceeded the limit it was given. Whether it counts toward a failure threshold is the threshold's to say.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "TimedOut", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TimedOutStateType : ExecutionStateTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimedOutStateType"/> class.
    /// </summary>
    public TimedOutStateType()
        : base(
            name: "TimedOut",
            displayName: "Timed out",
            isTerminal: true,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: System.Array.Empty<string>())
    {
    }
}
