using System;
using Fdw.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Sessions.Csrf;

/// <summary>
/// An <see cref="ISessionCsrfProtection"/> that mints its own token pair rather than relying on the
/// session store to mint one (FDW-945).
/// </summary>
/// <remarks>
/// <see cref="OriginCheckCsrfProtection"/> and <see cref="SynchronizerTokenCsrfProtection"/> both
/// verify against material the session store already holds — an allowed-origins list or a hash on
/// the session row — so nothing issues anything on their behalf. A protection backed by ASP.NET
/// Core's own <c>IAntiforgery</c> needs its token pair minted through that service instead, so this
/// interface is additive: <see cref="ISessionCsrfProtection"/> is unchanged, and
/// <c>SessionCookieDelivery.IssueCsrfToken</c> checks for this capability rather than assuming every
/// protection issues the same way.
/// </remarks>
public interface ICsrfTokenIssuer
{
    /// <summary>Issues a token for the current request and returns the value to hand the caller.</summary>
    /// <param name="context">The request issuing the token.</param>
    /// <param name="sessionId">The session the token is issued for, for logging only.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The token value, or a failure naming what went wrong.</returns>
    IGenericResult<string> IssueToken(HttpContext context, Guid sessionId, ILogger logger);
}
