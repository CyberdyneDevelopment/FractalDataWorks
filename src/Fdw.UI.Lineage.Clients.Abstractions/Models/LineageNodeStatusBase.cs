using Fdw.Collections;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// Base class for lineage node statuses.
/// </summary>
public abstract class LineageNodeStatusBase : TypeOptionBase<int, LineageNodeStatusBase>, ILineageNodeStatus
{
    /// <summary>
    /// Initializes a new instance of <see cref="LineageNodeStatusBase"/>.
    /// </summary>
    protected LineageNodeStatusBase(int id, string name) : base(id, name) { }
}
