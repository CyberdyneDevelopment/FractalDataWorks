using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Scheduled - execution is scheduled but not yet triggered. A run refused before it is triggered (a bad parameter, a role it lacks) fails from here (FDW-2034).
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Scheduled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ScheduledStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Triggered", "AwaitingApproval", "Failed", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledStateType"/> class.
    /// </summary>
    public ScheduledStateType()
        : base(
            name: "Scheduled",
            displayName: "Scheduled",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: ValidTransitionsArray)
    {
    }
}