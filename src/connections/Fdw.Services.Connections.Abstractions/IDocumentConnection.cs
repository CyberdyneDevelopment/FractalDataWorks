using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Documents.Abstractions;
using Fdw.Documents.Abstractions.Capabilities;
using Fdw.Documents.Abstractions.Commands;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A connection that stores whole files rather than records (FDW-1496): the sibling of <see cref="IDataConnection"/>. It is found
/// through the same connection type collection and provider, with the same credential handling and health probe (it is a
/// <see cref="IGenericConnection"/>); what it is asked is a document command, and where it may reach is its scope.
/// </summary>
/// <remarks>
/// Why a connection of its own and not a data connection that takes more commands: a connection that stores records has nothing
/// to answer a file command with, and one that stores files has no container to run a data command on (the capability doctrine of
/// FDW-1354). This package holds the shape only; a consumer supplies the implementation for its own storage.
/// </remarks>
public interface IDocumentConnection : IGenericConnection
{
    /// <summary>Gets where the connection may reach: the account, a container, or a folder within one.</summary>
    DocumentScope Scope { get; }

    /// <summary>Gets what this connection can do beyond the commands every document connection answers.</summary>
    IReadOnlyCollection<IDocumentCapability> Capabilities { get; }

    /// <summary>Runs a document command.</summary>
    /// <remarks>
    /// The implementation resolves every path through <see cref="Scope"/> before it reaches the storage, answers an anticipated failure
    /// (not found, already exists, outside scope, too large, a content type not allowed, read links not supported, the storage
    /// refusing) as a failed result carrying a <c>DocumentResultCodes</c> code, and never lets a storage exception escape as one.
    /// </remarks>
    /// <typeparam name="T">What the command answers with.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The command's answer, or why it was refused or failed.</returns>
    Task<IGenericResult<T>> Execute<T>(IDocumentCommand<T> command, CancellationToken cancellationToken = default);
}
