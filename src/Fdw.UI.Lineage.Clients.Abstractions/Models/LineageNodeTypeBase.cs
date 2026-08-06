using Fdw.Collections;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// Base class for lineage node types.
/// </summary>
public abstract class LineageNodeTypeBase : TypeOptionBase<int, LineageNodeTypeBase>, ILineageNodeType
{
    /// <summary>
    /// Initializes a new instance of <see cref="LineageNodeTypeBase"/>.
    /// </summary>
    protected LineageNodeTypeBase(int id, string name) : base(id, name) { }
}
