using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to remove trailing whitespace from lines.
/// </summary>
[TypeOption(typeof(RoslynCommands), "RemoveTrailingWhitespace")]
public sealed class RemoveTrailingWhitespaceCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveTrailingWhitespaceCommand"/> class.
    /// </summary>
    public RemoveTrailingWhitespaceCommand()
        : base("RemoveTrailingWhitespace", RoslynCommandCategories.Formatting, "Strip trailing whitespace from every line in the loaded solution. Use as a cleanup pass; safe to run repeatedly. Returns the count of files changed.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
