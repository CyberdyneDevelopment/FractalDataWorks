using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to format an entire document.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FormatDocument")]
public sealed class FormatDocumentCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatDocumentCommand"/> class.
    /// </summary>
    public FormatDocumentCommand()
        : base("FormatDocument", RoslynCommandCategories.Formatting, "Apply standard Roslyn formatting (indentation, spacing, line breaks) to a single document. Use as a cleanup step after large mechanical edits. Returns the changed-regions count.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
