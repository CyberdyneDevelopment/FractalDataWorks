using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// Moves a server-side session between the store and the browser (FDW-903): sets its cookie when a
/// session-delivered flow completes, ends it at logout, and hands out its CSRF token.
/// </summary>
/// <remarks>
/// The one place an endpoint goes for anything that touches the session cookie, so every sign-in path
/// sets the same cookie the same way, with attributes read from the Session service the flow names.
/// </remarks>
public interface ISessionCookieDelivery
{
    /// <summary>
    /// Sets the session cookie for the product of a completed session-delivered flow.
    /// </summary>
    /// <param name="context">The response to set the cookie on.</param>
    /// <param name="flow">The flow that completed. It must name a Session service.</param>
    /// <param name="session">The flow's product: <see cref="IssuedToken.AccessToken"/> is the session id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult> Deliver(
        HttpContext context, AuthenticationFlow flow, IssuedToken session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the session that authenticated this request with the product of a session-delivered
    /// flow the request ran (a tenant switch): the old session is revoked and the cookie now names
    /// the new one.
    /// </summary>
    /// <param name="context">The request, authenticated by a session.</param>
    /// <param name="flow">The flow that completed. It must name a Session service.</param>
    /// <param name="session">The flow's product: the new session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult> Replace(
        HttpContext context, AuthenticationFlow flow, IssuedToken session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends the session that authenticated this request, and clears its cookie.
    /// </summary>
    /// <param name="context">The request, authenticated by a session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or a failure when the request was not authenticated by a session.</returns>
    Task<IGenericResult> End(HttpContext context, CancellationToken cancellationToken = default);

    /// <summary>Issues a fresh CSRF token for the session that authenticated this request.</summary>
    /// <param name="context">The request, authenticated by a session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The token, which the browser app echoes on each write.</returns>
    Task<IGenericResult<string>> IssueCsrfToken(HttpContext context, CancellationToken cancellationToken = default);

    /// <summary>Whether this request was authenticated by a server-side session.</summary>
    /// <param name="context">The request.</param>
    /// <returns><see langword="true"/> when its principal came from a Session scheme.</returns>
    bool IsSessionRequest(HttpContext context);
}
