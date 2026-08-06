using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Scheduled - execution is scheduled but not yet triggered.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Scheduled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ScheduledStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Triggered", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduledStateType"/> class.
    /// </summary>
    public ScheduledStateType()
        : base(
            id: 1,
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