using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Lineage;

/// <summary>
/// Base class for lineage node types using CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class LineageNodeTypeBase : TypeOptionBase<int, LineageNodeTypeBase>, ILineageNodeType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineageNodeTypeBase"/> class.
    /// </summary>
    protected LineageNodeTypeBase(int id, string name) : base(id, name) { }
}
