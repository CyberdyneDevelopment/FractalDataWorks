using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Graph layout - nodes and edges with relationships.
/// </summary>
/// <remarks>
/// <para>
/// Represents data organized as a network of nodes connected by edges/relationships.
/// </para>
/// <para>
/// Examples: Neo4j graphs, RDF triples, social networks, knowledge graphs.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>SupportsNesting: true - Can contain nested node properties</item>
/// <item>SupportsFlattening: false - Graph structure cannot be easily flattened</item>
/// <item>IsTabular: false - Not a native row/column format</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(DataLayouts), "Graph")]
[ExcludeFromCodeCoverage]
public sealed class GraphLayout : DataLayoutBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphLayout"/> class.
    /// </summary>
    public GraphLayout()
        : base(
            id: 5,
            name: "Graph",
            description: "Nodes and edges (Neo4j, relationships)",
            supportsNesting: true,
            supportsFlattening: false,
            isTabular: false)
    {
    }
}
