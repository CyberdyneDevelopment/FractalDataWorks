using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Running - execution is actively in progress.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Running", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RunningStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Completed", "Failed", "Paused", "Retrying", "Compensating", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="RunningStateType"/> class.
    /// </summary>
    public RunningStateType()
        : base(
            id: 10,
            name: "Running",
            displayName: "Running",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: true,
            validTransitions: ValidTransitionsArray)
    {
    }
}