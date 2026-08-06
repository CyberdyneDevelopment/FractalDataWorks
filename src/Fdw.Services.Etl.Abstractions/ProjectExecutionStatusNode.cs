using System;
using System.Collections.Generic;
using Fdw.Operations.Abstractions.Execution;

namespace Fdw.Services.Etl.Projects.Abstractions;

/// <summary>
/// A node in the hierarchical project execution status tree.
/// Represents a Project, Stage, Step, or Pipeline execution item with its children.
/// </summary>
public sealed class ProjectExecutionStatusNode
{
    /// <summary>Gets or sets the underlying execution item for this node.</summary>
    public IExecutionItem ExecutionItem { get; set; } = null!;

    /// <summary>Gets or sets the depth of this node in the tree (0 = Project).</summary>
    public int Depth { get; set; }

    /// <summary>Gets or sets the display ordinal within its parent (for Stage and Step levels).</summary>
    public int? Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the rollup state name derived from child states.
    /// For leaf nodes, equals <see cref="IExecutionItem.State"/> name.
    /// For parent nodes, the most severe child state wins.
    /// </summary>
    public string RollupState { get; set; } = string.Empty;

    /// <summary>Gets or sets the child nodes (Stages under Project, Steps under Stage, Pipelines under Step).</summary>
    // Why: IList<T> for mutability during recursive tree construction; consumers receive the sealed class.
    public IList<ProjectExecutionStatusNode> Children { get; set; } = new List<ProjectExecutionStatusNode>();
}
