using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to normalize line endings in a document.
/// </summary>
[TypeOption(typeof(RoslynCommands), "NormalizeLineEndings")]
public sealed class NormalizeLineEndingsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NormalizeLineEndingsCommand"/> class.
    /// </summary>
    public NormalizeLineEndingsCommand()
        : base("NormalizeLineEndings", RoslynCommandCategories.Formatting, "Normalize line endings (CRLF / LF / CR) across the loaded solution to the target style. Use to enforce a consistent line-ending policy across mixed-OS contributions. Returns the count of files changed.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the target line ending (lf, crlf, cr).
    /// </summary>
    public string LineEnding { get; set; } = "lf";
}
