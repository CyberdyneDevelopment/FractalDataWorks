using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the path of a <see cref="CreateReadLinkCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "CreateReadLink")]
public sealed class CreateReadLinkNarrowingCommandHandler : NarrowingCommandHandlerBase<CreateReadLinkCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CreateReadLinkNarrowingCommandHandler"/> class.</summary>
    public CreateReadLinkNarrowingCommandHandler()
        : base("CreateReadLink")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(CreateReadLinkCommand command, DocumentScope folder) =>
        NarrowedPaths.Below(folder, command.Path, path => new CreateReadLinkCommand(path, command.Lifetime));
}
