using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to list all references in a project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ListReferences")]
public sealed class ListReferencesCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListReferencesCommand"/> class.
    /// </summary>
    public ListReferencesCommand()
        : base("ListReferences", RoslynCommandCategories.Project, "List all references (both NuGet PackageReferences and ProjectReferences) for a project. Use to inspect a project's dependency surface or to verify a refactor didn't drop expected references. Returns ReferenceListResult with each reference's name, version, and kind.")
    {
    }

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
}
