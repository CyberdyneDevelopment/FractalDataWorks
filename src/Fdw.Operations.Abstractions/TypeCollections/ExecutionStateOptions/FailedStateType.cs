using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Failed - execution finished with an error.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Failed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FailedStateType : ExecutionStateTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedStateType"/> class.
    /// </summary>
    public FailedStateType()
        : base(
            id: 21,
            name: "Failed",
            displayName: "Failed",
            isTerminal: true,
            isSuccess: false,
            isFailure: true,
            canTriggerEscalation: true,
            validTransitions: System.Array.Empty<string>())
    {
    }
}