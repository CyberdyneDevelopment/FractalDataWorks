using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the path of a <see cref="DeleteCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "Delete")]
public sealed class DeleteNarrowingCommandHandler : NarrowingCommandHandlerBase<DeleteCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DeleteNarrowingCommandHandler"/> class.</summary>
    public DeleteNarrowingCommandHandler()
        : base("Delete")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(DeleteCommand command, DocumentScope folder) =>
        NarrowedPaths.Below(folder, command.Path, path => new DeleteCommand(path));
}
