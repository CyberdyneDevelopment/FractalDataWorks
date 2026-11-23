using Fdw.Collections;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Defines a strategy for executing federated queries.
/// </summary>
/// <remarks>
/// Inherits Id, Name, and Description from ITypeOption.
/// </remarks>
public interface IFederationStrategy : ITypeOption<int, FederationStrategyBase>
{
    /// <summary>
    /// Gets whether this strategy executes source queries in parallel.
    /// </summary>
    bool IsParallel { get; }

    /// <summary>
    /// Gets whether this strategy optimizes query execution order.
    /// </summary>
    bool OptimizesOrder { get; }

    /// <summary>
    /// Gets the read limit this strategy declares: how many sources it reads at once when it reads in parallel. The declared default of the
    /// MaxParallelReads option; the FDW-level setting and a data set's own value win over it (FDW-6901).
    /// </summary>
    int MaxParallelReads { get; }
}
