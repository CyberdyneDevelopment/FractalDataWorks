using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Paused - execution has been temporarily suspended.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Paused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PausedStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Running", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="PausedStateType"/> class.
    /// </summary>
    public PausedStateType()
        : base(
            id: 11,
            name: "Paused",
            displayName: "Paused",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: true,
            validTransitions: ValidTransitionsArray)
    {
    }
}