using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code analysis operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
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
