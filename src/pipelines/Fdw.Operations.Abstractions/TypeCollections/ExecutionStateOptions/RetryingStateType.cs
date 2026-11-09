using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// Retrying - attempting retry after transient failure.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "Retrying", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RetryingStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Running", "Failed", "Cancelled", "Interrupted", "TimedOut" };

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryingStateType"/> class.
    /// </summary>
    public RetryingStateType()
        : base(
            name: "Retrying",
            displayName: "Retrying",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            isExecuting: true,
            canTriggerEscalation: true,
            validTransitions: ValidTransitionsArray)
    {
    }
}