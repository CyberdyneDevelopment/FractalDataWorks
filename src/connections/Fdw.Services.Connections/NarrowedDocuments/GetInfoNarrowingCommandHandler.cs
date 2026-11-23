using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Places the path of a <see cref="GetInfoCommand"/> under the folder.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NarrowingCommandHandlers), "GetInfo")]
public sealed class GetInfoNarrowingCommandHandler : NarrowingCommandHandlerBase<GetInfoCommand>
{
    /// <summary>Initializes a new instance of the <see cref="GetInfoNarrowingCommandHandler"/> class.</summary>
    public GetInfoNarrowingCommandHandler()
        : base("GetInfo")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<IDocumentCommand> Rewrite(GetInfoCommand command, DocumentScope folder) =>
        NarrowedPaths.Below(folder, command.Path, path => new GetInfoCommand(path));
}
