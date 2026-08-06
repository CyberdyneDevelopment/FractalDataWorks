using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to remove a project reference.
/// </summary>
[TypeOption(typeof(RoslynCommands), "RemoveProjectReference")]
public sealed class RemoveProjectReferenceCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveProjectReferenceCommand"/> class.
    /// </summary>
    public RemoveProjectReferenceCommand()
        : base("RemoveProjectReference", RoslynCommandCategories.Project, "Remove a project-to-project reference. Use to break a dependency edge in the solution graph. Returns RemoveProjectReferenceResult.")
    {
    }

    /// <summary>
    /// Gets or sets the name of the project to modify.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the project reference to remove.
    /// </summary>
    public string ReferenceName { get; set; } = string.Empty;
}
