using Fdw.Collections;

namespace Fdw.Schema;

/// <summary>
/// Interface for data layout types.
/// </summary>
/// <remarks>
/// <para>
/// DataLayouts classify the structural organization of data,
/// enabling polymorphic schema handling across different storage paradigms
/// (tabular, hierarchical, document, key-value, graph).
/// </para>
/// <para>
/// Extends ITypeOption to enable MutableTypeCollection pattern with source generator discovery.
/// </para>
/// </remarks>
public interface IDataLayout : ITypeOption<int, DataLayoutBase>
{
    /// <summary>
    /// Gets the description of this layout.
    /// </summary>
    /// <value>A human-readable description of the layout's structural characteristics.</value>
    string Description { get; }

    /// <summary>
    /// Gets a value indicating whether this layout supports nesting (hierarchical structures).
    /// </summary>
    /// <value>True if this layout can contain child schemas or nested elements; otherwise, false.</value>
    bool SupportsNesting { get; }

    /// <summary>
    /// Gets a value indicating whether this layout can be flattened to a tabular structure.
    /// </summary>
    /// <value>True if this layout can be transformed into a flat row/column format; otherwise, false.</value>
    bool SupportsFlattening { get; }

    /// <summary>
    /// Gets a value indicating whether this layout has a native row/column structure.
    /// </summary>
    /// <value>True if this layout naturally represents data as rows and columns; otherwise, false.</value>
    bool IsTabular { get; }
}
