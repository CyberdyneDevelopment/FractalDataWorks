using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Execute commands sequentially, one after another.
/// Output of command N becomes input to command N+1.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(ExecutionStrategies), "Sequential", RestrictToCurrentCompilation = true)]
public sealed class SequentialExecutionStrategy : ExecutionStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialExecutionStrategy"/> class.
    /// </summary>
    public SequentialExecutionStrategy()
        : base(
            id: 1,
            name: "Sequential",
            isSequential: true,
            stopOnFailure: true,  // Default behavior - stop on failure
            supportsParallel: false)
    {
    }
}
