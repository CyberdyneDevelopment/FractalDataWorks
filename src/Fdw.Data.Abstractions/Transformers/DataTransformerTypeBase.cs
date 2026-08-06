using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Base class for data transformer type definitions.
/// Provides metadata about ETL transformers.
/// </summary>
public abstract class DataTransformerTypeBase : TypeOptionBase<int, DataTransformerTypeBase>, IDataTransformerType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataTransformerTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this transformer type.</param>
    /// <param name="name">The name of this transformer type.</param>
    /// <param name="displayName">The display name for this transformer type.</param>
    /// <param name="description">The description of this transformer type.</param>
    /// <param name="supportsStreaming">Whether this transformer supports streaming.</param>
    /// <param name="category">The category for this transformer type (defaults to "Transformer").</param>
    protected DataTransformerTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        bool supportsStreaming,
        string? category = null)
        : base(id, name, $"Transformers:{name}", displayName, description, category ?? "Transformer")
    {
        SupportsStreaming = supportsStreaming;
    }

    /// <inheritdoc/>
    public bool SupportsStreaming { get; }
}
