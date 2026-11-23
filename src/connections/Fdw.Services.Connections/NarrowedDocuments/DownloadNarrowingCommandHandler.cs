using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the path of a <see cref="DownloadCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "Download")]
public sealed class DownloadNarrowingCommandHandler : NarrowingCommandHandlerBase<DownloadCommand>
{
    /// <summary>Initializes a new instance of the <see cref="DownloadNarrowingCommandHandler"/> class.</summary>
    public DownloadNarrowingCommandHandler()
        : base("Download")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(DownloadCommand command, DocumentScope folder) =>
        NarrowedPaths.Below(folder, command.Path, path => new DownloadCommand(path, command.Range));
}
