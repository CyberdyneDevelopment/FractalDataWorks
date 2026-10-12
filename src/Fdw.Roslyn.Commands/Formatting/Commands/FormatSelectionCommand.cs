using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to format a selection within a document.
/// </summary>
[TypeOption(typeof(RoslynCommands), "FormatSelection")]
public sealed class FormatSelectionCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormatSelectionCommand"/> class.
    /// </summary>
    public FormatSelectionCommand()
        : base("FormatSelection", RoslynCommandCategories.Formatting, "Apply Roslyn formatting to a selected range of a document. Use to format only the lines touched by an edit, preserving the rest. Returns the changed-regions count.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the start line number (1-based).
    /// </summary>
    public int StartLine { get; set; }
    /// <summary>
    /// Gets or sets the start column number (1-based).
    /// </summary>
    public int StartColumn { get; set; }
    /// <summary>
    /// Gets or sets the end line number (1-based).
    /// </summary>
    public int EndLine { get; set; }
    /// <summary>
    /// Gets or sets the end column number (1-based).
    /// </summary>
    public int EndColumn { get; set; }
}
