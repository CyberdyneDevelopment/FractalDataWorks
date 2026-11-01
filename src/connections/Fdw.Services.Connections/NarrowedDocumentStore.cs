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
    /// <param name="logger">The logger.</param>
    public NarrowedDocumentStore(IDocumentConnection connection, DocumentPath folder, ILogger<NarrowedDocumentStore>? logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _folder = new DocumentScope(folder);
        Scope = new DocumentScope(DocumentPath.Root);
        _logger = logger ?? NullLogger<NarrowedDocumentStore>.Instance;
    }

    /// <inheritdoc />
    public DocumentScope Scope { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDocumentCapability> Capabilities => _connection.Capabilities;

    /// <inheritdoc />
    public async Task<IGenericResult<T>> Execute<T>(IDocumentCommand<T> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rewritten = Rewrite(command);
        if (rewritten.IsFailure || rewritten.Value is not IDocumentCommand<T> inner)
        {
            return rewritten.IsFailure ? rewritten.ToNewResult<T>() : GenericResult<T>.Failure(Refused(command));
        }

        var result = await _connection.Execute(inner, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess && result.Value is { } value ? Narrow(value, result) : result;
    }

    private IGenericMessage Refused(IDocumentCommand command) => DocumentStoreLog.CommandNotNarrowable(_logger, command.GetType().Name, _folder.Root.Value);

    // The command with its paths placed under the folder; a type this store does not know is refused.
    private IGenericResult<IDocumentCommand> Rewrite(IDocumentCommand command) => command switch
    {
        UploadCommand c => Below(c.Path, path => new UploadCommand(path, c.Content, c.ContentType, c.CreateOnly, c.Metadata)),
        DownloadCommand c => Below(c.Path, path => new DownloadCommand(path, c.Range)),
        DeleteCommand c => Below(c.Path, path => new DeleteCommand(path)),
        GetInfoCommand c => Below(c.Path, path => new GetInfoCommand(path)),
        ListCommand c => Below(c.Folder, path => new ListCommand(path, c.Recursive, c.PageSize, c.Continuation)),
        CreateReadLinkCommand c => Below(c.Path, path => new CreateReadLinkCommand(path, c.Lifetime)),
        CopyCommand c => BelowBoth(c.Source, c.Destination, (source, destination) => new CopyCommand(source, destination, c.CreateOnly)),
        MoveCommand c => BelowBoth(c.Source, c.Destination, (source, destination) => new MoveCommand(source, destination, c.CreateOnly)),
        _ => GenericResult<IDocumentCommand>.Failure(Refused(command)),
    };

    private IGenericResult<IDocumentCommand> Below(DocumentPath path, Func<DocumentPath, IDocumentCommand> build)
    {
        var resolved = _folder.Resolve(path);
        return resolved.IsFailure ? resolved.ToNewResult<IDocumentCommand>() : GenericResult<IDocumentCommand>.Success(build(resolved.Value));
    }

    private IGenericResult<IDocumentCommand> BelowBoth(DocumentPath first, DocumentPath second, Func<DocumentPath, DocumentPath, IDocumentCommand> build)
    {
        var one = _folder.Resolve(first);
        var other = _folder.Resolve(second);
        if (one.IsFailure)
        {
            return one.ToNewResult<IDocumentCommand>();
        }

        return other.IsFailure ? other.ToNewResult<IDocumentCommand>() : GenericResult<IDocumentCommand>.Success(build(one.Value, other.Value));
    }

    // The result with its paths made relative to the folder again; results that carry no path are returned as they are.
    private IGenericResult<T> Narrow<T>(T value, IGenericResult<T> original) => value switch
    {
        DocumentInfo info => Typed<T, DocumentInfo>(RelativeInfo(info)),
        DocumentPage page => Typed<T, DocumentPage>(RelativePage(page)),
        DocumentContent content => Typed<T, DocumentContent>(RelativeContent(content)),
        _ => original,
    };

    private IGenericResult<DocumentInfo> RelativeInfo(DocumentInfo info)
    {
        var relative = _folder.Relative(info.Path);
        return relative.IsFailure
            ? relative.ToNewResult<DocumentInfo>()
            : GenericResult<DocumentInfo>.Success(new DocumentInfo(
                relative.Value, info.Length, info.ContentType, info.LastModified, info.ContentHash, info.Version, info.Metadata));
    }

    private IGenericResult<DocumentPage> RelativePage(DocumentPage page)
    {
        var items = new List<DocumentInfo>(page.Items.Count);
        foreach (var item in page.Items)
        {
            var relative = RelativeInfo(item);
            if (relative.IsFailure || relative.Value is null)
            {
                return relative.ToNewResult<DocumentPage>();
            }

            items.Add(relative.Value);
        }

        return GenericResult<DocumentPage>.Success(new DocumentPage(items, page.Continuation));
    }

    private IGenericResult<DocumentContent> RelativeContent(DocumentContent content)
    {
        var relative = RelativeInfo(content.Info);
        if (relative.IsFailure || relative.Value is null)
        {
            content.Dispose();
            return relative.ToNewResult<DocumentContent>();
        }

        return GenericResult<DocumentContent>.Success(new DocumentContent(relative.Value, content.Content));
    }

    // The narrowed value is of T by construction: the switch above matched the value of T to a concrete type, and a narrowed result is of that type.
    private static IGenericResult<T> Typed<T, TNarrowed>(IGenericResult<TNarrowed> narrowed)
        => narrowed.IsSuccess && narrowed.Value is T value ? GenericResult<T>.Success(value) : narrowed.ToNewResult<T>();
}
