using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for code search commands (find usages, implementations, duplicates, etc.).
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "CodeSearch", RestrictToCurrentCompilation = true)]
public sealed class CodeSearchCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeSearchCommandCategory"/> class.
    /// </summary>
    public CodeSearchCommandCategory()
        : base(8, "CodeSearch", "Semantic code search commands for finding usages, implementations, and duplicates")
    {
    }
}
