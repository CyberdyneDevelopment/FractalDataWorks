using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for compilation commands (build, emit, diagnostics, syntax validation, etc.).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Compilation", RestrictToCurrentCompilation = true)]
public sealed class CompilationCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationCommandCategory"/> class.
    /// </summary>
    public CompilationCommandCategory()
        : base(2, "Compilation", "Compilation commands for building, emitting assemblies, and syntax validation")
    {
    }
}
