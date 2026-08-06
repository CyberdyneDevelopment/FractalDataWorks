using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Triggered - execution has been triggered and is preparing to start.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Triggered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TriggeredStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Initialized", "Failed", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="TriggeredStateType"/> class.
    /// </summary>
    public TriggeredStateType()
        : base(
            id: 2,
            name: "Triggered",
            displayName: "Triggered",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: ValidTransitionsArray)
    {
    }
}