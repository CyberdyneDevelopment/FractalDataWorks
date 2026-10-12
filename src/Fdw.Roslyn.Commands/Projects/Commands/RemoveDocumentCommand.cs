using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to remove a document from a project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "RemoveDocument")]
public sealed class RemoveDocumentCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveDocumentCommand"/> class.
    /// </summary>
    public RemoveDocumentCommand()
        : base("RemoveDocument", RoslynCommandCategories.Project, "Remove a document from a project by file path or document ID. Use to delete a file from the project's compilation set; deletes the file on disk only if explicitly requested. Returns RemoveDocumentResult.")
    {
    }

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document file path or name.
    /// </summary>
    public string DocumentPath { get; set; } = string.Empty;
}
