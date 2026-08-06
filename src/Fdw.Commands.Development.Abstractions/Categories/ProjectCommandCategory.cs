using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for project management commands (add/remove documents, references, etc.).
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Project", RestrictToCurrentCompilation = true)]
public sealed class ProjectCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectCommandCategory"/> class.
    /// </summary>
    public ProjectCommandCategory()
        : base(6, "Project", "Project management commands for documents, references, and project information")
    {
    }
}
