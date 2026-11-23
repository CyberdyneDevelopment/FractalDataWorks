using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the paths of a <see cref="MoveCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "Move")]
public sealed class MoveNarrowingCommandHandler : NarrowingCommandHandlerBase<MoveCommand>
{
    /// <summary>Initializes a new instance of the <see cref="MoveNarrowingCommandHandler"/> class.</summary>
    public MoveNarrowingCommandHandler()
        : base("Move")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(MoveCommand command, DocumentScope folder) =>
        NarrowedPaths.BelowBoth(folder, command.Source, command.Destination, (source, destination) => new MoveCommand(source, destination, command.CreateOnly));
}
