using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to add a project reference.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AddProjectReference")]
public sealed class AddProjectReferenceCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddProjectReferenceCommand"/> class.
    /// </summary>
    public AddProjectReferenceCommand()
        : base("AddProjectReference", RoslynCommandCategories.Project, "Add a project-to-project reference from one project to another. Use to establish a dependency edge in the solution graph. Returns AddProjectReferenceResult with success status and any restore warnings.")
    {
    }

    /// <summary>
    /// Gets or sets the name of the project to modify.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the project to reference.
    /// </summary>
    public string ReferenceName { get; set; } = string.Empty;
}
