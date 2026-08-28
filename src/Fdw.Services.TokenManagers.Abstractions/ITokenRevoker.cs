using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Ends what issuance began.
/// </summary>
/// <remarks>
/// Neither flow-time nor request-time — revocation and logout happen on their own occasions, and
/// bundling them with issuance meant a resource server inherited the ability to revoke.
/// <para>
/// Named for revocation rather than for sessions. <c>Fdw.Workspace.Roslyn</c> held the
/// <c>ISessionManager</c> name for workspace sessions — since renamed to
/// <c>IRoslynSessionManager</c> — and two unrelated interfaces sharing a name in one solution is a
/// reader checking the namespace every time to find out which concept they are looking at. This one
/// does not manage anything in any case: it ends things.
/// </para>
/// </remarks>
public interface ITokenRevoker
{
    /// <summary>Revokes a single token.</summary>
    /// <param name="token">The token to revoke.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult> Revoke(string token, CancellationToken cancellationToken = default);

    /// <summary>Ends every session for a principal.</summary>
    /// <param name="principalId">The principal to log out.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult> Logout(Guid principalId, CancellationToken cancellationToken = default);
}
