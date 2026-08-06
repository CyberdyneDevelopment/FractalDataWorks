using System.Text.Json.Serialization;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to get baseline information.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetBaseline")]
public sealed class GetBaselineCommand : RoslynCommandBase, IBaselineAwareCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetBaselineCommand"/> class.
    /// </summary>
    public GetBaselineCommand()
        : base("GetBaseline", RoslynCommandCategories.Workspace, "Return information about the currently-set baseline (when it was set, what state was captured). Use to confirm the comparison anchor for CompareToBaseline. Returns BaselineInfo or null if no baseline is set.")
    {
    }

    /// <summary>
    /// Gets or sets the baseline solution.
    /// Set by the handler before translation; excluded from JSON because System.Text.Json's
    /// type analysis chokes on <see cref="Solution"/>'s transitive ref-struct properties at
    /// deserialization time.
    /// </summary>
    [JsonIgnore]
    public Solution? BaselineSolution { get; set; }
}
