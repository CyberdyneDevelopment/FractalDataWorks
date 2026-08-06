using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Refactoring.Commands;

/// <summary>
/// Command to expand <c>&lt;inheritdoc/&gt;</c> comments using Roslyn's own inheritdoc resolution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ResolveInheritDoc")]
public sealed class ResolveInheritDocCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResolveInheritDocCommand"/> class.
    /// </summary>
    public ResolveInheritDocCommand()
        : base(
            "ResolveInheritDoc",
            RoslynCommandCategories.Refactoring,
            "Expand <inheritdoc/> comments in place using Roslyn's documentation resolution: each site whose docs Roslyn can resolve (via the override/interface chain or an explicit cref) is rewritten to the concrete <summary>/<param>/<returns>/... tags, preserving the leading /// and indentation. Sites Roslyn cannot resolve are reported with file:line — these are the true MA0196 candidates needing hand-written docs. Mutation is in-memory; run ApplyWorkspaceChanges to persist. Scope with FilePath or ProjectName, or omit both to process the whole solution. Idempotent: resolved sites no longer contain <inheritdoc/>, unresolved sites are left untouched.")
    {
    }

    /// <summary>
    /// Gets or sets an optional source file path to restrict processing to a single file.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional project name to restrict processing to one project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
}
