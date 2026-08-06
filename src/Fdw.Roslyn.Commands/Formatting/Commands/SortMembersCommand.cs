using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to sort members within a type.
/// </summary>
[TypeOption(typeof(RoslynCommands), "SortMembers")]
public sealed class SortMembersCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortMembersCommand"/> class.
    /// </summary>
    public SortMembersCommand()
        : base("SortMembers", RoslynCommandCategories.Formatting, "Sort members within each type per the configured order (fields, ctors, properties, methods; access-modifier sub-order). Use to enforce a consistent member layout before review. Returns the changed-types count.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int Line { get; set; }
    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int Column { get; set; }
}
