using System.Text.Json.Serialization;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to revert workspace to baseline state.
/// </summary>
[TypeOption(typeof(RoslynCommands), "RevertToBaseline")]
public sealed class RevertToBaselineCommand : RoslynCommandBase, IBaselineAwareCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevertToBaselineCommand"/> class.
    /// </summary>
    public RevertToBaselineCommand()
        : base("RevertToBaseline", RoslynCommandCategories.Workspace, "Revert the workspace to the baseline state. Use as the 'undo everything since the baseline' command. Destructive — all changes since the baseline are discarded. Returns the count of documents reverted.")
    {
    }

    /// <summary>
    /// Gets or sets the baseline solution to revert to.
    /// Set by the handler before translation; excluded from JSON because System.Text.Json's
    /// type analysis chokes on <see cref="Solution"/>'s transitive ref-struct properties at
    /// deserialization time.
    /// </summary>
    [JsonIgnore]
    public Solution? BaselineSolution { get; set; }
}
