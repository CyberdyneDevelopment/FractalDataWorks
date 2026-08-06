using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for compilation operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Compilation", RestrictToCurrentCompilation = true)]
public sealed class CompilationCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationCommandCategory"/> class.
    /// </summary>
    public CompilationCommandCategory() : base(2, "Compilation", "Compilation operations")
    {
    }
}
