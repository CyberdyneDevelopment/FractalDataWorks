using Fdw.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.WriteModeOptions;

/// <summary>
/// Interface for write mode TypeOptions.
/// </summary>
/// <remarks>
/// Write modes define how data is written to a destination:
/// append to existing, overwrite, upsert (update or insert), or create new only.
/// </remarks>
public interface IWriteMode : ITypeOption<int, WriteModeBase>
{
    /// <summary>
    /// Gets whether this write mode requires checking if data already exists.
    /// </summary>
    bool RequiresExistenceCheck { get; }

    /// <summary>
    /// Gets whether this write mode preserves existing data.
    /// </summary>
    bool PreservesExistingData { get; }

    /// <summary>
    /// Gets whether this write mode can create new data.
    /// </summary>
    bool CanCreate { get; }

    /// <summary>
    /// Gets whether this write mode can update existing data.
    /// </summary>
    bool CanUpdate { get; }

    /// <summary>
    /// Gets whether this write mode can delete existing data.
    /// </summary>
    bool CanDelete { get; }

    /// <summary>
    /// Validates the write mode configuration for a specific destination.
    /// </summary>
    /// <param name="stageConfiguration">The stage configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating whether the configuration is valid.</returns>
    Task<IGenericResult> Validate(
        IGenericConfiguration stageConfiguration,
        CancellationToken cancellationToken = default);
}
