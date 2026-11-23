using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the paths of a <see cref="CopyCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "Copy")]
public sealed class CopyNarrowingCommandHandler : NarrowingCommandHandlerBase<CopyCommand>
{
    /// <summary>Initializes a new instance of the <see cref="CopyNarrowingCommandHandler"/> class.</summary>
    public CopyNarrowingCommandHandler()
        : base("Copy")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(CopyCommand command, DocumentScope folder) =>
        NarrowedPaths.BelowBoth(folder, command.Source, command.Destination, (source, destination) => new CopyCommand(source, destination, command.CreateOnly));
}
