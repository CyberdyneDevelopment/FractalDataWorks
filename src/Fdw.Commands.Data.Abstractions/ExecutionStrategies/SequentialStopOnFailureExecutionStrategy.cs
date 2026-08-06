using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Execute commands sequentially but stop on first failure.
/// Useful for critical pipelines where a failure should halt all subsequent operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ExecutionStrategies), "SequentialStopOnFailure", RestrictToCurrentCompilation = true)]
public sealed class SequentialStopOnFailureExecutionStrategy : ExecutionStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialStopOnFailureExecutionStrategy"/> class.
    /// </summary>
    public SequentialStopOnFailureExecutionStrategy()
        : base(
            id: 3,
            name: "SequentialStopOnFailure",
            isSequential: true,
            stopOnFailure: true,
            supportsParallel: false)
    {
    }
}
