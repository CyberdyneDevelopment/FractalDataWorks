using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.NodeTypes;

/// <summary>
/// An ETL project orchestration node — the root of the Project → Stage → Step → Pipeline hierarchy.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageNodeTypes), "Project")]
public sealed class ProjectNodeType : LineageNodeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ProjectNodeType"/> class.</summary>
    public ProjectNodeType() : base(6, "Project") { }
}
