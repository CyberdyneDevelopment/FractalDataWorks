using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for project management operations.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Project", RestrictToCurrentCompilation = true)]
public sealed class ProjectCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectCommandCategory"/> class.
    /// </summary>
    public ProjectCommandCategory() : base(7, "Project", "Project management operations")
    {
    }
}
