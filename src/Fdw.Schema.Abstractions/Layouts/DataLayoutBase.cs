using Fdw.Collections;

namespace Fdw.Schema;

/// <summary>
/// Base class for data layouts using CRTP pattern.
/// </summary>
/// <remarks>
/// <para>
/// Provides the foundation for all data layout implementations.
/// Each layout defines its structural characteristics: whether it supports nesting,
/// can be flattened, and whether it's naturally tabular.
/// </para>
/// <para>
/// Properties are set in constructor so TypeCollection source generator can read them
/// without instantiation.
/// </para>
/// </remarks>
public abstract class DataLayoutBase : TypeOptionBase<int, DataLayoutBase>, IDataLayout
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataLayoutBase"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for this layout.</param>
    /// <param name="name">Name of the layout (must match TypeOption attribute).</param>
    /// <param name="description">Human-readable description of the layout.</param>
    /// <param name="supportsNesting">Whether this layout supports hierarchical nesting.</param>
    /// <param name="supportsFlattening">Whether this layout can be flattened to tabular form.</param>
    /// <param name="isTabular">Whether this layout has a native row/column structure.</param>
    protected DataLayoutBase(
        int id,
        string name,
        string description,
        bool supportsNesting,
        bool supportsFlattening,
        bool isTabular)
        : base(id, name, $"DataLayouts:{name}", name, description, "DataLayout")
    {
        SupportsNesting = supportsNesting;
        SupportsFlattening = supportsFlattening;
        IsTabular = isTabular;
    }

    /// <summary>
    /// Gets a value indicating whether this layout supports nesting (hierarchical structures).
    /// </summary>
    /// <value>True if this layout can contain child schemas or nested elements; otherwise, false.</value>
    public bool SupportsNesting { get; }

    /// <summary>
    /// Gets a value indicating whether this layout can be flattened to a tabular structure.
    /// </summary>
    /// <value>True if this layout can be transformed into a flat row/column format; otherwise, false.</value>
    public bool SupportsFlattening { get; }

    /// <summary>
    /// Gets a value indicating whether this layout has a native row/column structure.
    /// </summary>
    /// <value>True if this layout naturally represents data as rows and columns; otherwise, false.</value>
    public bool IsTabular { get; }
}
