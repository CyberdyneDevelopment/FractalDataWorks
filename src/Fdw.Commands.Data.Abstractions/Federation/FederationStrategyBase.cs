using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Base class for federation strategy implementations.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class FederationStrategyBase : TypeOptionBase<int, FederationStrategyBase>, IFederationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FederationStrategyBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this strategy.</param>
    /// <param name="name">The name of this strategy.</param>
    /// <param name="description">The description of this strategy's behavior.</param>
    /// <param name="isParallel">Whether this strategy executes queries in parallel.</param>
    /// <param name="optimizesOrder">Whether this strategy optimizes query execution order.</param>
    protected FederationStrategyBase(
        int id,
        string name,
        string description,
        bool isParallel,
        bool optimizesOrder)
        : base(id, name, description)
    {
        IsParallel = isParallel;
        OptimizesOrder = optimizesOrder;
    }

    /// <inheritdoc/>
    public bool IsParallel { get; }

    /// <inheritdoc/>
    public bool OptimizesOrder { get; }
}
