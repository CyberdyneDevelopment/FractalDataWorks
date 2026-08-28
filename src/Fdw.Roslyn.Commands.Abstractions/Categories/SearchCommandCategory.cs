using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code search operations.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Search", RestrictToCurrentCompilation = true)]
public sealed class SearchCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchCommandCategory"/> class.
    /// </summary>
    public SearchCommandCategory() : base(9, "Search", "Code search operations")
    {
    }
}
