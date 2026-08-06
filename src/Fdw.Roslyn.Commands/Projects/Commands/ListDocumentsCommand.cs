using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to list all documents in a project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ListDocuments")]
public sealed class ListDocumentsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDocumentsCommand"/> class.
    /// </summary>
    public ListDocumentsCommand()
        : base("ListDocuments", RoslynCommandCategories.Project, "List every document in a project. Use as a directory-listing alternative when you want only the documents the project actually compiles (excluding excluded files, generated outputs). Returns DocumentListResult with file paths and IDs.")
    {
    }

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional file name pattern filter.
    /// </summary>
    public string? Pattern { get; set; }
}
