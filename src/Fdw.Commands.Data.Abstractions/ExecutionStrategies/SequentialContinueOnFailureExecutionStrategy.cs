using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Execute all commands even if some fail, then aggregate results.
/// Useful for best-effort scenarios where partial success is acceptable.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ExecutionStrategies), "SequentialContinueOnFailure", RestrictToCurrentCompilation = true)]
public sealed class SequentialContinueOnFailureExecutionStrategy : ExecutionStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialContinueOnFailureExecutionStrategy"/> class.
    /// </summary>
    public SequentialContinueOnFailureExecutionStrategy()
        : base(
            id: 4,
            name: "SequentialContinueOnFailure",
            isSequential: true,
            stopOnFailure: false,
            supportsParallel: false)
    {
    }
}
