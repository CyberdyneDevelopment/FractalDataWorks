using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for refactoring commands (rename, extract method, encapsulate field, etc.).
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Refactoring", RestrictToCurrentCompilation = true)]
public sealed class RefactoringCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefactoringCommandCategory"/> class.
    /// </summary>
    public RefactoringCommandCategory()
        : base(7, "Refactoring", "Code refactoring commands for renaming, extracting methods, and restructuring code")
    {
    }
}
