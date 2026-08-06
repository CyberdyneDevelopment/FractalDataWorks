using Fdw.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Resiliency.Abstractions;

namespace Fdw.Services.Resiliency;

/// <summary>
/// Base class for resiliency strategy TypeOptions.
/// Each concrete strategy (PollyRetry, PrimaryBackup, RetryNotify, None)
/// inherits from this and is registered via <c>[TypeOption(typeof(ResiliencyTypes), "StrategyName")]</c>.
/// </summary>
/// <remarks>
/// The TypeCollection holds one singleton prototype per type. The <see cref="Execute"/> method
/// is called by <see cref="IResiliencyExecutor"/> with the fully-resolved configuration and context.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: concrete strategies are tested individually
public abstract class ResiliencyTypeBase
    : TypeOptionBase<int, ResiliencyTypeBase>
    , IResiliencyType
{
    /// <summary>
    /// Parameterless constructor for the Empty/NotFound sentinel (source-generated).
    /// </summary>
    protected ResiliencyTypeBase()
        : base(0, string.Empty)
    {
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    /// <param name="id">Unique integer identifier within ResiliencyTypes.</param>
    /// <param name="name">The TypeOption name — matches the StrategyType discriminator in the database.</param>
    /// <param name="displayName">Human-readable display name.</param>
    /// <param name="description">Description of this strategy's behavior.</param>
    protected ResiliencyTypeBase(
        int id,
        string name,
        string displayName,
        string description)
        : base(id, name, $"Resiliency:{name}", displayName, description, "Resiliency")
    {
    }

    /// <inheritdoc/>
    public abstract Task<IGenericResult> Execute(
        Func<CancellationToken, Task<IGenericResult>> runStage,
        IGenericConfiguration config,
        IResiliencyExecutionContext ctx,
        CancellationToken cancellationToken);
}
