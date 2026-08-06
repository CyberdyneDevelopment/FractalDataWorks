using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Lineage;

/// <summary>
/// Base class for lineage node status types using CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class LineageNodeStatusBase : TypeOptionBase<int, LineageNodeStatusBase>, ILineageNodeStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineageNodeStatusBase"/> class.
    /// </summary>
    protected LineageNodeStatusBase(int id, string name) : base(id, name) { }
}
