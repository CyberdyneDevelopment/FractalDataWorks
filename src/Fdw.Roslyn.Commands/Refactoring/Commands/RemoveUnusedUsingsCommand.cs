using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to remove unused using directives from a file.
/// </summary>
[TypeOption(typeof(RoslynCommands), "RemoveUnusedUsings")]
public sealed class RemoveUnusedUsingsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveUnusedUsingsCommand"/> class.
    /// </summary>
    public RemoveUnusedUsingsCommand()
        : base("RemoveUnusedUsings", RoslynCommandCategories.Refactoring, "Remove using directives in a document that aren't referenced by the file's code. Use as a cleanup step after edits that removed types; safe to run repeatedly. Returns the removed-usings count.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
