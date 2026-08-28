using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code navigation operations.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Navigation", RestrictToCurrentCompilation = true)]
public sealed class NavigationCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationCommandCategory"/> class.
    /// </summary>
    public NavigationCommandCategory() : base(6, "Navigation", "Code navigation operations")
    {
    }
}
