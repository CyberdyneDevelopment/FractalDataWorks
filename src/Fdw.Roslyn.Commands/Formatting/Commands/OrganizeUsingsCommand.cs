using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to organize and sort using directives.
/// </summary>
[TypeOption(typeof(RoslynCommands), "OrganizeUsings")]
public sealed class OrganizeUsingsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizeUsingsCommand"/> class.
    /// </summary>
    public OrganizeUsingsCommand()
        : base("OrganizeUsings", RoslynCommandCategories.Formatting, "Sort and group using directives in a document per the standard style (System first, then alphabetical). Use after edits that added or removed usings. Returns the changed-files count.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets whether to place System namespaces first.
    /// </summary>
    public bool SystemFirst { get; set; } = true;
    /// <summary>
    /// Gets or sets whether to separate namespace groups with blank lines.
    /// </summary>
    public bool SeparateGroups { get; set; } = true;
}
