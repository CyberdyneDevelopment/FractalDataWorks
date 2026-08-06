using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code generation operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Generation", RestrictToCurrentCompilation = true)]
public sealed class GenerationCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerationCommandCategory"/> class.
    /// </summary>
    public GenerationCommandCategory() : base(5, "Generation", "Code generation operations")
    {
    }
}
