using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to add missing using directives to a file.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AddUsings")]
public sealed class AddUsingsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddUsingsCommand"/> class.
    /// </summary>
    public AddUsingsCommand()
        : base("AddUsings", RoslynCommandCategories.Refactoring, "Add missing using directives to a document based on currently-undefined types referenced in source. Use after an edit that introduced unqualified type names; safe to run repeatedly. Returns the added-usings count.")
    {
    }

    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}
