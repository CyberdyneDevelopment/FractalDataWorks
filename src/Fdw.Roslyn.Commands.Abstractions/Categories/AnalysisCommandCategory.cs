using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code analysis operations.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Analysis", RestrictToCurrentCompilation = true)]
public sealed class AnalysisCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisCommandCategory"/> class.
    /// </summary>
    public AnalysisCommandCategory() : base(1, "Analysis", "Code analysis operations")
    {
    }
}
