using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Initialized - execution resources have been allocated and validated.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Initialized", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InitializedStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Running", "Failed", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializedStateType"/> class.
    /// </summary>
    public InitializedStateType()
        : base(
            id: 3,
            name: "Initialized",
            displayName: "Initialized",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: ValidTransitionsArray)
    {
    }
}