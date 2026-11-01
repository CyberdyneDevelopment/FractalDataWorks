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
}
