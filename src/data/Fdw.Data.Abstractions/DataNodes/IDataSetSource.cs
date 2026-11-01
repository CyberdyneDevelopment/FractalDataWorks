namespace Fdw.Data.Abstractions;

/// <summary>
/// A named entry in an <see cref="IDataSet"/> that supplies rows from a single
/// <see cref="IDataNode"/> with an optional alias and pre-filter.
/// </summary>
public interface IDataSetSource
{
    /// <summary>
    /// Gets the data node (container or nested dataset) that supplies rows.
    /// </summary>
    IDataNode Node { get; }

    /// <summary>
    /// Gets the optional alias used to reference this source in join conditions and
    /// field bindings (e.g., SQL table alias).
    /// </summary>
    string? Alias { get; }

    /// <summary>
    /// Gets an optional filter that is applied to rows from <see cref="Node"/> before
    /// they participate in any join or projection.
    /// </summary>
    IFilterExpression? Filter { get; }
}
