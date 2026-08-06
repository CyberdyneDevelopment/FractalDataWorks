using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Parallel federation strategy - executes all source queries concurrently.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FederationStrategies), "Parallel", RestrictToCurrentCompilation = true)]
public sealed class ParallelStrategy : FederationStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelStrategy"/> class.
    /// </summary>
    public ParallelStrategy()
        : base(
            id: 1,
            name: "Parallel",
            description: "Executes all source queries concurrently for maximum performance",
            isParallel: true,
            optimizesOrder: false)
    {
    }
}
