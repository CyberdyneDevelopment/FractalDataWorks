using System;
using Fdw.Collections;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>Rebuilds one kind of document command with its paths placed under a folder; the collection is keyed by the command's <c>CommandType</c>.</summary>
public interface INarrowingCommandHandler : ITypeOption<int, INarrowingCommandHandler>
{
    /// <summary>Rewrites the command.</summary>
    /// <param name="command">The command, of the kind this handler is named for.</param>
    /// <param name="folder">The folder the paths are placed under.</param>
    /// <param name="refused">Makes the message for a command that is not of the type this handler rewrites; it logs when called.</param>
    /// <returns>The command with its paths under the folder, or the failure of a path that does not resolve.</returns>
    IGenericResult<IDocumentCommand> Rewrite(IDocumentCommand command, DocumentScope folder, Func<IGenericMessage> refused);
}
