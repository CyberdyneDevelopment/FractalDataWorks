using System.Text.Json.Serialization;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;

/// <summary>
/// Command to compare current workspace to baseline.
/// </summary>
[TypeOption(typeof(RoslynCommands), "CompareToBaseline")]
public sealed class CompareToBaselineCommand : RoslynCommandBase, IBaselineAwareCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompareToBaselineCommand"/> class.
    /// </summary>
    public CompareToBaselineCommand()
        : base("CompareToBaseline", RoslynCommandCategories.Workspace, "Compare the current workspace state to the last set baseline and report the differences (added, removed, modified documents). Use to see exactly what's changed since the baseline was captured. Returns a list of changes per file.")
    {
    }

    /// <summary>
    /// Gets or sets the baseline solution to compare against.
    /// Set by the handler before translation; excluded from JSON because System.Text.Json's
    /// type analysis chokes on <see cref="Solution"/>'s transitive ref-struct properties at
    /// deserialization time.
    /// </summary>
    [JsonIgnore]
    public Solution? BaselineSolution { get; set; }
}
