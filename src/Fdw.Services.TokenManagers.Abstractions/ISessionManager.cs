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
/// </remarks>
public interface ISessionManager
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
