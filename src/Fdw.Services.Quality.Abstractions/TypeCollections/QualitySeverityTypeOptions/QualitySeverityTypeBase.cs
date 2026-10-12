using Fdw.Collections;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions;

/// <summary>
/// Base class for quality severity types using the CRTP pattern.
/// </summary>
public abstract class QualitySeverityTypeBase : TypeOptionBase<int, QualitySeverityTypeBase>, IQualitySeverityType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualitySeverityTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The unique name.</param>
    /// <param name="priority">The priority level (lower is higher priority).</param>
    /// <param name="blocksProcessing">Whether violations of this severity block processing.</param>
    protected QualitySeverityTypeBase(int id, string name, int priority, bool blocksProcessing)
        : base(id, name)
    {
        Priority = priority;
        BlocksProcessing = blocksProcessing;
    }

    /// <inheritdoc/>
    public int Priority { get; }

    /// <inheritdoc/>
    public bool BlocksProcessing { get; }
}
