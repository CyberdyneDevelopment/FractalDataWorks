using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for code navigation commands (find definition, base types, members, etc.).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Navigation", RestrictToCurrentCompilation = true)]
public sealed class NavigationCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationCommandCategory"/> class.
    /// </summary>
    public NavigationCommandCategory()
        : base(5, "Navigation", "Code navigation commands for finding definitions, base types, and members")
    {
    }
}
