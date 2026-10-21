using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Capabilities;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// An organization's document storage for one purpose (FDW-1581): a document connection narrowed to the folder the organization was
/// granted, so that a caller can reach only what is under it.
/// </summary>
/// <remarks>
/// <para>
/// It is not an <see cref="IDocumentConnection"/> on purpose. The connection itself is never handed to a caller, because a caller
/// holding it could name any path in it. A store takes the same document commands, with every path relative to the store's own root,
/// and answers with paths relative to that root. Whatever path a caller passes, it cannot reach outside the folder.
/// </para>
/// </remarks>
public interface IDocumentStore
{
    /// <summary>Gets the scope of the store: its root is the store's own root, so a caller's paths are checked against the folder the store was narrowed to.</summary>
    DocumentScope Scope { get; }

    /// <summary>Gets what the underlying connection can do.</summary>
    IReadOnlyCollection<IDocumentCapability> Capabilities { get; }

    /// <summary>Executes a document command inside the store.</summary>
    /// <typeparam name="T">The result of the command.</typeparam>
    /// <param name="command">The command; its paths are relative to the store's root.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, with paths relative to the store's root, or the reason the command was refused.</returns>
    Task<IGenericResult<T>> Execute<T>(IDocumentCommand<T> command, CancellationToken cancellationToken = default);
}
