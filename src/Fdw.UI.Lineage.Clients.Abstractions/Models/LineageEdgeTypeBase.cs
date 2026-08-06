using Fdw.Collections;

namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// Base class for lineage edge types.
/// </summary>
public abstract class LineageEdgeTypeBase : TypeOptionBase<int, LineageEdgeTypeBase>, ILineageEdgeType
{
    /// <summary>
    /// Initializes a new instance of <see cref="LineageEdgeTypeBase"/>.
    /// </summary>
    protected LineageEdgeTypeBase(int id, string name) : base(id, name) { }
}
