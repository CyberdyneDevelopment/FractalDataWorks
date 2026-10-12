using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Execute commands in parallel where possible.
/// Requires commands to be independent (no data dependencies).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ExecutionStrategies), "Parallel", RestrictToCurrentCompilation = true)]
public sealed class ParallelExecutionStrategy : ExecutionStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelExecutionStrategy"/> class.
    /// </summary>
    public ParallelExecutionStrategy()
        : base(
            id: 2,
            name: "Parallel",
            isSequential: false,
            stopOnFailure: false,  // Parallel execution continues until all tasks complete
            supportsParallel: true)
    {
    }
}
