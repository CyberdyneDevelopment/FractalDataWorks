using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for workspace commands (snapshots, baseline, workspace info, etc.).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Workspace", RestrictToCurrentCompilation = true)]
public sealed class WorkspaceCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceCommandCategory"/> class.
    /// </summary>
    public WorkspaceCommandCategory()
        : base(9, "Workspace", "Workspace management commands for snapshots, baselines, and solution state")
    {
    }
}
