using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.ServiceTypes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Base class for federation strategy implementations.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
/// <remarks>
/// A strategy declares what it does as its defaults (FDW-6901, <see cref="IDeclaresDefaults"/>): <see cref="FederationStrategyDefaults.ReadsInParallel"/>,
/// <see cref="FederationStrategyDefaults.MaxParallelReads"/> and <see cref="FederationStrategyDefaults.OrdersSources"/>. A host changes one with
/// <c>ServiceTypeDefaults.Override</c> before it registers, the host's configuration wins over both, and a data set's own value wins over all.
/// </remarks>
[ExcludeFromCodeCoverage]
public abstract class FederationStrategyBase : TypeOptionBase<FederationStrategyBase>, IFederationStrategy, IDeclaresDefaults
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FederationStrategyBase"/> class.
    /// </summary>
    /// <param name="name">The name of this strategy.</param>
    /// <param name="description">The description of this strategy's behavior.</param>
    /// <param name="isParallel">Whether this strategy executes queries in parallel.</param>
    /// <param name="optimizesOrder">Whether this strategy optimizes query execution order.</param>
    /// <param name="maxParallelReads">The declared read limit: how many sources it reads at once when it reads in parallel; at least one (FDW-6901).</param>
    protected FederationStrategyBase(
        string name,
        string description,
        bool isParallel,
        bool optimizesOrder,
        int maxParallelReads)
        : base(name, description)
    {
        IsParallel = isParallel;
        OptimizesOrder = optimizesOrder;
        MaxParallelReads = maxParallelReads;
        DeclaredDefaults = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [FederationStrategyDefaults.ReadsInParallel] = isParallel,
            [FederationStrategyDefaults.MaxParallelReads] = maxParallelReads,
            [FederationStrategyDefaults.OrdersSources] = optimizesOrder,
        };
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> DeclaredDefaults { get; }

    /// <inheritdoc/>
    public bool IsParallel { get; }

    /// <inheritdoc/>
    public bool OptimizesOrder { get; }

    /// <inheritdoc />
    public int MaxParallelReads { get; }
}
