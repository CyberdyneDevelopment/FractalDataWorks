using Fdw.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.StageTypeOptions;

/// <summary>
/// Interface for pipeline stage type TypeOptions.
/// </summary>
/// <remarks>
/// Stage types define the behavior and requirements for different kinds of pipeline stages.
/// Each type specifies what resources it needs (source, destination) and execution characteristics.
/// </remarks>
public interface IStageType : ITypeOption<int, StageTypeBase>
{
    /// <summary>
    /// Gets whether this stage type requires a source connection.
    /// </summary>
    bool RequiresSource { get; }

    /// <summary>
    /// Gets whether this stage type requires a destination connection.
    /// </summary>
    bool RequiresDestination { get; }

    /// <summary>
    /// Gets whether this stage type can execute in parallel with other stages.
    /// </summary>
    bool SupportsParallel { get; }

    /// <summary>
    /// Gets whether this stage type produces output data.
    /// </summary>
    bool ProducesOutput { get; }

    /// <summary>
    /// Gets whether this stage type consumes input data.
    /// </summary>
    bool ConsumesInput { get; }

    /// <summary>
    /// Validates the stage configuration for this stage type.
    /// </summary>
    /// <param name="configuration">The stage configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the configuration is valid.</returns>
    Task<IGenericResult> ValidateConfiguration(
        IGenericConfiguration configuration,
        CancellationToken cancellationToken = default);
}
