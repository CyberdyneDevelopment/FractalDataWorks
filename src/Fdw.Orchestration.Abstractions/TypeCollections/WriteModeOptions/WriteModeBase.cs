using Fdw.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Orchestration.Pipelines.Abstractions.TypeCollections.WriteModeOptions;

/// <summary>
/// Base class for write mode TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for write modes used in pipeline load stages.
/// Derived classes implement specific write behaviors.
/// </remarks>
public abstract class WriteModeBase : TypeOptionBase<int, WriteModeBase>, IWriteMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WriteModeBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="requiresExistenceCheck">Whether this mode requires checking if data exists.</param>
    /// <param name="preservesExistingData">Whether this mode preserves existing data.</param>
    /// <param name="canCreate">Whether this mode can create new data.</param>
    /// <param name="canUpdate">Whether this mode can update existing data.</param>
    /// <param name="canDelete">Whether this mode can delete existing data.</param>
    protected WriteModeBase(
        int id,
        string name,
        bool requiresExistenceCheck,
        bool preservesExistingData,
        bool canCreate = true,
        bool canUpdate = false,
        bool canDelete = false)
        : base(id, name)
    {
        RequiresExistenceCheck = requiresExistenceCheck;
        PreservesExistingData = preservesExistingData;
        CanCreate = canCreate;
        CanUpdate = canUpdate;
        CanDelete = canDelete;
    }

    /// <inheritdoc/>
    public bool RequiresExistenceCheck { get; }

    /// <inheritdoc/>
    public bool PreservesExistingData { get; }

    /// <inheritdoc/>
    public bool CanCreate { get; }

    /// <inheritdoc/>
    public bool CanUpdate { get; }

    /// <inheritdoc/>
    public bool CanDelete { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult> Validate(
        IGenericConfiguration stageConfiguration,
        CancellationToken cancellationToken = default);
}
