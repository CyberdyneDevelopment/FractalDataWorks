using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Sessions;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Ends the platform session a SQL login holds once the connection that holds it is gone.
/// </summary>
/// <remarks>
/// A SQL login's session lives exactly as long as its connection. Left to idle out, every pooled, abandoned or
/// refused connection would leave behind a session that still opens the platform after its client had gone.
/// </remarks>
internal static class TdsSessionClosing
{
    /// <summary>Revokes the session a login delivered.</summary>
    /// <param name="sessions">The session store.</param>
    /// <param name="sessionToken">The session id the login flow delivered.</param>
    /// <param name="serviceName">The session service it belongs to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once no live session remains, or the failure reading or revoking it.</returns>
    public static async Task<IGenericResult> Close(ISessionStore sessions, string sessionToken, string serviceName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        var used = await sessions.Use(sessionToken, serviceName, cancellationToken).ConfigureAwait(false);
        if (used.IsFailure)
            return used;

        // No live session under the id: nothing is left that opens the platform.
        if (used.Value is not { } session)
            return used;

        return await sessions.Revoke(session.Id, cancellationToken).ConfigureAwait(false);
    }
}
