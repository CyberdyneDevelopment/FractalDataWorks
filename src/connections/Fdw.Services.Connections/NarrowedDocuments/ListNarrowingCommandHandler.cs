using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the path of a <see cref="ListCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "List")]
public sealed class ListNarrowingCommandHandler : NarrowingCommandHandlerBase<ListCommand>
{
    /// <summary>Initializes a new instance of the <see cref="ListNarrowingCommandHandler"/> class.</summary>
    public ListNarrowingCommandHandler()
        : base("List")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(ListCommand command, DocumentScope folder) =>
        NarrowedPaths.Below(folder, command.Folder, path => new ListCommand(path, command.Recursive, command.PageSize, command.Continuation));
}
