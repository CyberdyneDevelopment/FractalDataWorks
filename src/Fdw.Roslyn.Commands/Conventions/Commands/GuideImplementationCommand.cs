using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Conventions.Commands;
/// <summary>
/// Command to get the implementation guide for a type or a project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GuideImplementation")]
public sealed class GuideImplementationCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuideImplementationCommand"/> class.
    /// </summary>
    public GuideImplementationCommand()
        : base("GuideImplementation", RoslynCommandCategories.Conventions, "Answer 'how do I create an implementation of this, what must be in it, what must not, and which real one do I copy?' for a TypeName, a ProjectName, or a type inside a project. Guides come from fdw-anatomy's implementing document (GuidesPath is its content folder) and are matched by which abstractions the target implements or the project references. Adds live facts from the solution: the abstraction's XML documentation, members the type leaves unimplemented, and where each exemplar is now. Fails with NoImplementationGuide when no guide applies; there is no generic fallback guide.")
    {
    }

    /// <summary>
    /// Gets or sets the path to the folder holding implementing.json.
    /// </summary>
    [System.ComponentModel.Description("Required. Path to the fdw-anatomy content folder that holds implementing.json and implementing.meta.json.")]
    public string? GuidesPath { get; init; }

    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    [System.ComponentModel.Description("Type to guide: a simple name for a type declared in the solution, or a namespace-qualified name for a type from a referenced package. Give this or ProjectName.")]
    public string? TypeName { get; init; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    [System.ComponentModel.Description("Project to guide, or to scope the TypeName search to. A project is matched by the abstractions it references. Give this or TypeName.")]
    public string? ProjectName { get; init; }
}
