using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the path of a <see cref="UploadCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "Upload")]
public sealed class UploadNarrowingCommandHandler : NarrowingCommandHandlerBase<UploadCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UploadNarrowingCommandHandler"/> class.</summary>
    public UploadNarrowingCommandHandler()
        : base("Upload")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(UploadCommand command, DocumentScope folder) =>
        NarrowedPaths.Below(folder, command.Path, path => new UploadCommand(path, command.Content, command.ContentType, command.CreateOnly, command.Metadata));
}
