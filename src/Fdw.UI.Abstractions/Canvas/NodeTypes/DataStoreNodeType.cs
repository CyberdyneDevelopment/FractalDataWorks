using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Canvas.NodeTypes;

/// <summary>
/// A logical data store node (e.g. database, file share, API endpoint group).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CanvasNodeTypes), "DataStore")]
public sealed class DataStoreNodeType : CanvasNodeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreNodeType"/> class.
    /// </summary>
    public DataStoreNodeType()
        : base(2, "DataStore", "Data Store", "Infrastructure", "database")
    {
    }
}
