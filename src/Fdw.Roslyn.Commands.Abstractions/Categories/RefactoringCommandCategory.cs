using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code refactoring operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Refactoring", RestrictToCurrentCompilation = true)]
public sealed class RefactoringCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefactoringCommandCategory"/> class.
    /// </summary>
    public RefactoringCommandCategory() : base(8, "Refactoring", "Code refactoring operations")
    {
    }
}
