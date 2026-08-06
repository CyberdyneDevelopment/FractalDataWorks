using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Represents a data transformer type definition - metadata about ETL transformers.
/// </summary>
/// <remarks>
/// Transformer types apply ETL-style transformations (aggregation, filtering, calculations, etc.).
/// </remarks>
public interface IDataTransformerType : ITypeOption<int>
{
    /// <summary>
    /// Gets the configuration key for this transformer type value.
    /// </summary>
    string ConfigurationKey { get; }

    /// <summary>
    /// Gets the display name for this transformer type value.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description of this transformer type value.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets whether this transformer supports streaming.
    /// </summary>
    bool SupportsStreaming { get; }
}
