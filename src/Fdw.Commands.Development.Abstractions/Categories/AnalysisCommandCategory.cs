using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for code analysis commands (complexity, dependencies, diagnostics, etc.).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Analysis", RestrictToCurrentCompilation = true)]
public sealed class AnalysisCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisCommandCategory"/> class.
    /// </summary>
    public AnalysisCommandCategory()
        : base(1, "Analysis", "Code analysis commands for complexity, dependencies, diagnostics, and symbol information")
    {
    }
}
