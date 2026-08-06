using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Optimized federation strategy - analyzes and chooses best execution approach.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FederationStrategies), "Optimized", RestrictToCurrentCompilation = true)]
public sealed class OptimizedStrategy : FederationStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OptimizedStrategy"/> class.
    /// </summary>
    public OptimizedStrategy()
        : base(
            id: 3,
            name: "Optimized",
            description: "Analyzes query characteristics and chooses optimal execution approach",
            isParallel: true,
            optimizesOrder: true)
    {
    }
}
