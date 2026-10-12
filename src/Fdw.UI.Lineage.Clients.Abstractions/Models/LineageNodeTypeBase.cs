using Fdw.Collections;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// Base class for lineage node types.
/// </summary>
public abstract class LineageNodeTypeBase : TypeOptionBase<LineageNodeTypeBase>, ILineageNodeType
{
    /// <summary>
    /// Initializes a new instance of <see cref="LineageNodeTypeBase"/>.
    /// </summary>
    protected LineageNodeTypeBase(string name) : base(name) { }
}
