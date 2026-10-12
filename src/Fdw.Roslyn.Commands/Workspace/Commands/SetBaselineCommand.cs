using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;
/// <summary>
/// Command to set the baseline for change detection.
/// </summary>
[TypeOption(typeof(RoslynCommands), "SetBaseline")]
public sealed class SetBaselineCommand : RoslynCommandBase, IBaselineSettingCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetBaselineCommand"/> class.
    /// </summary>
    public SetBaselineCommand()
        : base("SetBaseline", RoslynCommandCategories.Workspace, "Set the current workspace state as the comparison baseline. Use to mark a starting point before a series of edits, so CompareToBaseline can later show the cumulative diff. Returns the baseline ID and timestamp.")
    {
    }
}
