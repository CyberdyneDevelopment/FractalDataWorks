using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Command category for code formatting operations.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynCommandCategories), "Formatting", RestrictToCurrentCompilation = true)]
public sealed class FormattingCommandCategory : RoslynCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingCommandCategory"/> class.
    /// </summary>
    public FormattingCommandCategory() : base(4, "Formatting", "Code formatting operations")
    {
    }
}
