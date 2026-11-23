using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Messages;
using Fdw.Results;

namespace Fdw.Services.Connections.NarrowedDocuments;

/// <summary>The type the narrowing command handler collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class NarrowingCommandHandlerBase : TypeOptionBase<NarrowingCommandHandlerBase>, INarrowingCommandHandler
{
    /// <summary>Initializes the handler with the command type it rewrites.</summary>
    /// <param name="name">The command's <c>CommandType</c>.</param>
    protected NarrowingCommandHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult<IDocumentCommand> Rewrite(IDocumentCommand command, DocumentScope folder, Func<IGenericMessage> refused);
}

/// <summary>A narrowing command handler for one command class.</summary>
/// <typeparam name="TCommand">The command class the handler rewrites.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class NarrowingCommandHandlerBase<TCommand> : NarrowingCommandHandlerBase
    where TCommand : class, IDocumentCommand
{
    /// <summary>Initializes the handler with the command type it rewrites.</summary>
    /// <param name="name">The command's <c>CommandType</c>.</param>
    protected NarrowingCommandHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public sealed override IGenericResult<IDocumentCommand> Rewrite(IDocumentCommand command, DocumentScope folder, Func<IGenericMessage> refused) =>
        command is TCommand typed ? Rewrite(typed, folder) : GenericResult<IDocumentCommand>.Failure(refused());

    /// <summary>Rewrites the command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="folder">The folder the paths are placed under.</param>
    /// <returns>The command with its paths under the folder, or the failure of a path that does not resolve.</returns>
    protected abstract IGenericResult<IDocumentCommand> Rewrite(TCommand command, DocumentScope folder);
}
