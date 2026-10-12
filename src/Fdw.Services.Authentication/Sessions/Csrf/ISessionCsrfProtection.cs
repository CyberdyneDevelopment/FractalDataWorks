using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Sessions.Csrf;

/// <summary>
/// One way of proving that a cookie-authenticated write was made on purpose by this host's own
/// browser app, and not by another site riding the cookie the browser sends on its own (FDW-903).
/// </summary>
/// <remarks>
/// A bearer token is never sent by the browser unasked, so bearer requests need none of this. A
/// session cookie is, so every write that a session authenticates passes the protection its Session
/// service names. Reads (GET, HEAD, OPTIONS, TRACE) are not checked: they change nothing, and
/// SameSite plus CORS keep another site from reading the answer.
/// </remarks>
public interface ISessionCsrfProtection : ITypeOption<int>
{
    /// <summary>Checks, at startup, that a Session service's row carries what this protection needs.</summary>
    /// <param name="configuration">The Session service's implementation row.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Success, or a failure naming what is missing or contradictory.</returns>
    IGenericResult ValidateConfiguration(ISessionAuthenticationConfiguration configuration, ILogger logger);

    /// <summary>Checks one cookie-authenticated write.</summary>
    /// <param name="request">The write.</param>
    /// <param name="session">The session that authenticated it.</param>
    /// <param name="configuration">The Session service's implementation row.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success, or a failure that refuses the request.</returns>
    /// <remarks>
    /// Async because a protection is free to call out for its check (FDW-945: the Antiforgery option
    /// calls ASP.NET Core's own <c>IAntiforgery</c>, whose API is async-only). A protection with
    /// nothing to await returns <see cref="Task.FromResult{TResult}"/> rather than paying for an
    /// <c>async</c> state machine it does not need.
    /// </remarks>
    Task<IGenericResult> Verify(
        HttpRequest request, SessionRecord session, ISessionAuthenticationConfiguration configuration, ILogger logger,
        CancellationToken cancellationToken = default);
}
