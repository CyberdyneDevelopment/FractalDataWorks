using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for refactoring commands (rename, extract method, encapsulate field, etc.).
/// </summary>
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
