using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions.Categories;

/// <summary>
/// Category for code formatting commands (format document, organize imports, etc.).
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(DevelopmentCommandCategories), "Formatting", RestrictToCurrentCompilation = true)]
public sealed class FormattingCommandCategory : DevelopmentCommandCategoryBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingCommandCategory"/> class.
    /// </summary>
    public FormattingCommandCategory()
        : base(3, "Formatting", "Code formatting commands for document formatting, import organization, and style normalization")
    {
    }
}
