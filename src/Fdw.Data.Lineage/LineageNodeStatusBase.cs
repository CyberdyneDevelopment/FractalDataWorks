using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Lineage;

/// <summary>
/// Base class for lineage node status types using CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class LineageNodeStatusBase : TypeOptionBase<LineageNodeStatusBase>, ILineageNodeStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineageNodeStatusBase"/> class.
    /// </summary>
    protected LineageNodeStatusBase(string name) : base(name) { }
}
