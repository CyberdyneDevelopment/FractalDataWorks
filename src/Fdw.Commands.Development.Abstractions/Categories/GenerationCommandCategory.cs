using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for code generation commands (generate class, method, tests, etc.).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Generation", RestrictToCurrentCompilation = true)]
public sealed class GenerationCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationCommandCategory"/> class.
    /// </summary>
    public GenerationCommandCategory()
        : base(4, "Generation", "Code generation commands for creating classes, methods, tests, and documentation")
    {
    }
}
