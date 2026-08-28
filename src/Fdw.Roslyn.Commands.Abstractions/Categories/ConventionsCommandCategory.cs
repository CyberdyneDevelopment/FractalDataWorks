using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for FDW convention validation operations.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Conventions", RestrictToCurrentCompilation = true)]
public sealed class ConventionsCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConventionsCommandCategory"/> class.
    /// </summary>
    public ConventionsCommandCategory() : base(3, "Conventions", "FDW convention validation operations")
    {
    }
}
