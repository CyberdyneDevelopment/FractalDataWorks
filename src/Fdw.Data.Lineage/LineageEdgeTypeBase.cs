using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Lineage;

/// <summary>
/// Base class for lineage edge types using CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class LineageEdgeTypeBase : TypeOptionBase<int, LineageEdgeTypeBase>, ILineageEdgeType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LineageEdgeTypeBase"/> class.
    /// </summary>
    protected LineageEdgeTypeBase(int id, string name) : base(id, name) { }
}
