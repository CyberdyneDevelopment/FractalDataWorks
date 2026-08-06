using Fdw.Collections;

namespace Fdw.Orchestration.Abstractions.TypeCollections.ValidationSeverityOptions;

/// <summary>
/// Base class for validation severity TypeOptions.
/// </summary>
/// <remarks>
/// Provides common functionality for validation severity levels used to
/// classify the importance of validation results.
/// </remarks>
public abstract class ValidationSeverityBase : TypeOptionBase<int, ValidationSeverityBase>, IValidationSeverity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationSeverityBase"/> class.
    /// </summary>
    /// <param name="id">Unique numeric identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="level">Severity level for ordering (higher = more severe).</param>
    /// <param name="blocksExecution">Whether this severity blocks execution.</param>
    /// <param name="requiresAcknowledgment">Whether acknowledgment is required to proceed.</param>
    /// <param name="shouldLog">Whether this severity should be logged.</param>
    protected ValidationSeverityBase(
        int id,
        string name,
        int level,
        bool blocksExecution,
        bool requiresAcknowledgment = false,
        bool shouldLog = true)
        : base(id, name)
    {
        Level = level;
        BlocksExecution = blocksExecution;
        RequiresAcknowledgment = requiresAcknowledgment;
        ShouldLog = shouldLog;
    }

    /// <inheritdoc/>
    public int Level { get; }

    /// <inheritdoc/>
    public bool BlocksExecution { get; }

    /// <inheritdoc/>
    public bool RequiresAcknowledgment { get; }

    /// <inheritdoc/>
    public bool ShouldLog { get; }
}
