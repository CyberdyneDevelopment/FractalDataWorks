using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Cancelled - execution was cancelled before completion.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Cancelled", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CancelledStateType : ExecutionStateTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CancelledStateType"/> class.
    /// </summary>
    public CancelledStateType()
        : base(
            id: 22,
            name: "Cancelled",
            displayName: "Cancelled",
            isTerminal: true,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: System.Array.Empty<string>())
    {
    }
}