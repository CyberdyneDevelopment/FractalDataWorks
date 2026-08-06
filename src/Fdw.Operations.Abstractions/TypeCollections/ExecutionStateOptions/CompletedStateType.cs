using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Completed - execution finished successfully.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Completed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CompletedStateType : ExecutionStateTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompletedStateType"/> class.
    /// </summary>
    public CompletedStateType()
        : base(
            id: 20,
            name: "Completed",
            displayName: "Completed",
            isTerminal: true,
            isSuccess: true,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: System.Array.Empty<string>())
    {
    }
}