using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for workspace management operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Workspace", RestrictToCurrentCompilation = true)]
public sealed class WorkspaceCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceCommandCategory"/> class.
    /// </summary>
    public WorkspaceCommandCategory() : base(10, "Workspace", "Workspace management operations")
    {
    }
}
