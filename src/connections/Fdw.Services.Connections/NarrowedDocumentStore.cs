using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Capabilities;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Logging;
using Fdw.Services.Connections.NarrowedDocuments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// A document connection narrowed to a folder (FDW-1581): the caller's paths are relative to the folder, and nothing outside it can be
/// reached.
/// </summary>
/// <remarks>
/// <para>
/// Each command is rebuilt with its paths placed under the folder, through <see cref="DocumentScope.Resolve(DocumentPath)"/> so the
/// containment check is the one every document path goes through, and is run on the connection. Paths in the result are made relative to
/// the folder again; a path the connection returns outside the folder fails the result rather than leaking.
/// </para>
/// <para>
/// A command type the store does not know how to narrow is refused, never passed through: a new kind of command would otherwise reach the
/// whole connection.
/// </para>
/// </remarks>
public sealed class NarrowedDocumentStore : IDocumentStore
{
    private readonly IDocumentConnection _connection;
    private readonly DocumentScope _folder;
    private readonly ILogger<NarrowedDocumentStore> _logger;

    /// <summary>Initializes a new instance of the <see cref="NarrowedDocumentStore"/> class.</summary>
    /// <param name="connection">The granted connection, which never leaves the store.</param>
    /// <param name="folder">The folder the store is narrowed to, relative to the connection's scope.</param>
    /// <param name="storedUnder">The id of the assignment the store was resolved through (FDW-3396).</param>
    /// <param name="logger">The logger.</param>
    public NarrowedDocumentStore(IDocumentConnection connection, DocumentPath folder, Guid storedUnder, ILogger<NarrowedDocumentStore>? logger)
    {
        _connection = connection;
        StoredUnder = storedUnder;
        _folder = new DocumentScope(folder);
        Scope = new DocumentScope(DocumentPath.Root);
        _logger = logger ?? NullLogger<NarrowedDocumentStore>.Instance;
    }

    /// <inheritdoc />
    public DocumentScope Scope { get; }

    /// <inheritdoc />
    public Guid StoredUnder { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDocumentCapability> Capabilities => _connection.Capabilities;

    /// <inheritdoc />
    public async Task<IGenericResult<T>> Execute<T>(IDocumentCommand<T> command, CancellationToken cancellationToken = default)
    {
        var rewritten = Rewrite(command);
        if (rewritten.IsFailure || rewritten.Value is not IDocumentCommand<T> inner)
        {
            return rewritten.IsFailure ? rewritten.ToNewResult<T>() : GenericResult<T>.Failure(Refused(command));
        }

        var result = await _connection.Execute(inner, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess && result.Value is { } value ? Narrow(command, (object)value, result) : result;
    }

    private IGenericMessage Refused(IDocumentCommand command) => DocumentStoreLog.CommandNotNarrowable(_logger, command.GetType().Name, _folder.Root.Value);

    // The command with its paths placed under the folder; a type this store does not know is refused.
    private IGenericResult<IDocumentCommand> Rewrite(IDocumentCommand command)
    {
        var handler = NarrowingCommandHandlers.ByName(command.CommandType);
        return ReferenceEquals(handler, NarrowingCommandHandlers.NotFound)
            ? GenericResult<IDocumentCommand>.Failure(Refused(command))
            : handler.Rewrite(command, _folder, () => Refused(command));
    }

    // The result with its paths made relative to the folder again; results that carry no path are returned as they are.
    private IGenericResult<T> Narrow<T>(IDocumentCommand command, object value, IGenericResult<T> original)
    {
        var handler = NarrowedResultHandlers.ByName(value.GetType().Name);
        if (ReferenceEquals(handler, NarrowedResultHandlers.NotFound))
        {
            return original;
        }

        var narrowed = handler.Narrow(value, _folder, () => Refused(command));
        return narrowed.IsSuccess && narrowed.Value is T narrowedValue ? GenericResult<T>.Success(narrowedValue) : narrowed.ToNewResult<T>();
    }
}
