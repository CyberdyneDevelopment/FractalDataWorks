using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to add a document to a project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "AddDocument")]
public sealed class AddDocumentCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddDocumentCommand"/> class.
    /// </summary>
    public AddDocumentCommand()
        : base("AddDocument", RoslynCommandCategories.Project, "Add a new document (file) to a project, optionally with initial content. Use to add a code file to a project without touching the .csproj by hand. Returns AddDocumentResult with the new document ID and file path.")
    {
    }

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the document.
    /// </summary>
    public string DocumentName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the document content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the folder path.
    /// </summary>
    public IReadOnlyList<string> Folders { get; set; } = System.Array.Empty<string>();
}
