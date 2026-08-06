using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Sequential federation strategy - executes source queries one at a time.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(FederationStrategies), "Sequential", RestrictToCurrentCompilation = true)]
public sealed class SequentialStrategy : FederationStrategyBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SequentialStrategy"/> class.
    /// </summary>
    public SequentialStrategy()
        : base(
            id: 2,
            name: "Sequential",
            description: "Executes source queries one at a time in order",
            isParallel: false,
            optimizesOrder: false)
    {
    }
}
