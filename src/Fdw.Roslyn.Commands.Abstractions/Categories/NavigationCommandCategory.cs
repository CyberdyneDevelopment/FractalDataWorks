using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code navigation operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
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
