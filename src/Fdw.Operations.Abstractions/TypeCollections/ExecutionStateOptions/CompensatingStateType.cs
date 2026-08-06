using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Compensating - execution is running compensation/rollback logic.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Compensating", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompensatingStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Failed", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="CompensatingStateType"/> class.
    /// </summary>
    public CompensatingStateType()
        : base(
            id: 12,
            name: "Compensating",
            displayName: "Compensating",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: true,
            validTransitions: ValidTransitionsArray)
    {
    }
}