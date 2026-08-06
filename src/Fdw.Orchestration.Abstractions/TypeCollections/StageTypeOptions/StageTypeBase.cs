using Fdw.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions;

/// <summary>
/// Base class for pipeline stage type TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for stage types. Derived classes implement
/// specific stage type behaviors (Extract, Transform, Load, etc.).
/// </remarks>
public abstract class StageTypeBase : TypeOptionBase<int, StageTypeBase>, IStageType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageTypeBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="requiresSource">Whether this stage requires a source connection.</param>
    /// <param name="requiresDestination">Whether this stage requires a destination connection.</param>
    /// <param name="supportsParallel">Whether this stage can execute in parallel.</param>
    /// <param name="producesOutput">Whether this stage produces output data.</param>
    /// <param name="consumesInput">Whether this stage consumes input data.</param>
    protected StageTypeBase(
        int id,
        string name,
        bool requiresSource,
        bool requiresDestination,
        bool supportsParallel,
        bool producesOutput = true,
        bool consumesInput = true)
        : base(id, name)
    {
        RequiresSource = requiresSource;
        RequiresDestination = requiresDestination;
        SupportsParallel = supportsParallel;
        ProducesOutput = producesOutput;
        ConsumesInput = consumesInput;
    }

    /// <inheritdoc/>
    public bool RequiresSource { get; }

    /// <inheritdoc/>
    public bool RequiresDestination { get; }

    /// <inheritdoc/>
    public bool SupportsParallel { get; }

    /// <inheritdoc/>
    public bool ProducesOutput { get; }

    /// <inheritdoc/>
    public bool ConsumesInput { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult> ValidateConfiguration(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default);
}
