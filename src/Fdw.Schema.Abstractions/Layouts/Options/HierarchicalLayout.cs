using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Hierarchical layout - nested parent-child structure.
/// </summary>
/// <remarks>
/// <para>
/// Represents data organized in a tree-like structure with parent-child relationships.
/// </para>
/// <para>
/// Examples: JSON arrays, XML elements, nested data structures.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>SupportsNesting: true - Can contain child schemas</item>
/// <item>SupportsFlattening: true - Can be flattened with path expressions</item>
/// <item>IsTabular: false - Not a native row/column format</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(DataLayouts), "Hierarchical")]
[ExcludeFromCodeCoverage]
public sealed class HierarchicalLayout : DataLayoutBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HierarchicalLayout"/> class.
    /// </summary>
    public HierarchicalLayout()
        : base(
            id: 2,
            name: "Hierarchical",
            description: "Nested parent-child structure (JSON, XML)",
            supportsNesting: true,
            supportsFlattening: true,
            isTabular: false)
    {
    }
}
