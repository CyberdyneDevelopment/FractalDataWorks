using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Sessions;

/// <summary>
/// Turns a presented session id into the principal it stands for, whatever carried the id.
/// </summary>
/// <remarks>
/// <para>
/// A session id arrives in a browser cookie or is held by a long-lived connection that signed in once
/// (a SQL Server login). Either way the same three things decide whether it still stands: the store
/// refuses a session that expired, was revoked or belongs to another service and slides its idle end;
/// the user-wide revocation that ends a user's bearer tokens ends the session too; and the principal
/// is built from the claim set baked at sign-in. Only what is specific to one transport — the cookie,
/// CSRF on a browser write — stays with that transport.
/// </para>
/// <para>
/// Every refusal fails closed: a store or revocation check that cannot answer is a refusal, never an
/// authenticated caller with nothing checked.
/// </para>
/// </remarks>
public interface ISessionAuthenticator
{
    /// <summary>Uses the session the id names and builds its principal.</summary>
    /// <param name="sessionId">The session id the caller presented.</param>
    /// <param name="serviceName">The session service the id must belong to.</param>
    /// <param name="authenticationType">The authentication type the principal's identity carries.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The live session and its principal, or the refusal.</returns>
    Task<IGenericResult<AuthenticatedSession>> Authenticate(
        string sessionId, string serviceName, string authenticationType, CancellationToken cancellationToken = default);
}
