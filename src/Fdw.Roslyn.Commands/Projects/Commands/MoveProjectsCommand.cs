using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to move one or more projects to different folders within the source tree.
/// Batch operation ensures inter-references between moved projects are computed correctly.
/// </summary>
[TypeOption(typeof(RoslynCommands), "MoveProjects")]
public sealed class MoveProjectsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveProjectsCommand"/> class.
    /// </summary>
    public MoveProjectsCommand()
        : base("MoveProjects", RoslynCommandCategories.Project,
               "Move one or more projects to new folders within the source tree as a batch. Use the batch form (not single-project moves) so inter-references between moved projects are recomputed correctly in one pass. Returns MoveProjectsResult per moved project with success status and any reference-rewriting warnings.")
    {
    }

    /// <summary>
    /// Gets or sets the list of project move specifications.
    /// </summary>
    public IReadOnlyList<ProjectMoveSpec> Moves { get; set; } = System.Array.Empty<ProjectMoveSpec>();
}
