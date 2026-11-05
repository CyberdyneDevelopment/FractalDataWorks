using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// The state of an execution the host stopped or lost (FDW-3787): the ETL host was stopped while it ran, or the process died. It is
/// not Failed (the work did not fail) and not Cancelled (nobody cancelled it).
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Interrupted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InterruptedStateType : ExecutionStateTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="InterruptedStateType"/> class.</summary>
    public InterruptedStateType()
        : base(
            name: "Interrupted",
            displayName: "Interrupted",
            isTerminal: true,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: System.Array.Empty<string>())
    {
    }
}
