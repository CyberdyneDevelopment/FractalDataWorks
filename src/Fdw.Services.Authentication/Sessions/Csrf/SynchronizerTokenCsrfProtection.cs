using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Sessions.Csrf;

/// <summary>
/// Accepts a cookie-authenticated write only when it echoes, in a header, the CSRF token issued to
/// its session.
/// </summary>
/// <remarks>
/// The synchronizer-token pattern, held on the server like the session itself: the session row keeps
/// only the SHA-256 of the current token, and the browser app fetches a fresh one over an
/// authenticated read. Another site can make the browser send the cookie, but it cannot read that
/// response, so it cannot know the token. No data-protection key ring is involved.
/// </remarks>
[TypeOption(typeof(SessionCsrfProtectionTypes), "SynchronizerToken")]
public sealed class SynchronizerTokenCsrfProtection : SessionCsrfProtectionBase
{
    /// <summary>Initializes a new instance of the <see cref="SynchronizerTokenCsrfProtection"/> class.</summary>
    public SynchronizerTokenCsrfProtection()
        : base("SynchronizerToken")
    {
    }

    /// <inheritdoc />
    public override IGenericResult ValidateConfiguration(ISessionAuthenticationConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.AllowedOrigins is not null)
            return GenericResult.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, "AllowedOrigins is set, but SynchronizerToken checks no origin; set it only for OriginCheck"));

        return string.IsNullOrWhiteSpace(configuration.CsrfHeaderName)
            ? GenericResult.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, "SynchronizerToken names no CsrfHeaderName; name the header a write echoes its token in"))
            : GenericResult.Success();
    }

    /// <inheritdoc />
    public override Task<IGenericResult> Verify(
        HttpRequest request, SessionRecord session, ISessionAuthenticationConfiguration configuration, ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(configuration);

        if (session.CsrfTokenHash is not { } expected)
            return Task.FromResult<IGenericResult>(GenericResult.Failure(SessionLog.CsrfRefused(
                logger, Name, request.Method, request.Path, configuration.Name, "no CSRF token has been issued to this session")));

        var presented = request.Headers[configuration.CsrfHeaderName!].ToString();
        if (presented.Length == 0)
            return Task.FromResult<IGenericResult>(GenericResult.Failure(SessionLog.CsrfRefused(
                logger, Name, request.Method, request.Path, configuration.Name, $"the request carries no '{configuration.CsrfHeaderName}' header")));

        // Fixed-time: comparing hashes byte by byte and stopping at the first difference would let a
        // caller learn how much of a guess was right.
        if (!CryptographicOperations.FixedTimeEquals(DatabaseSessionStore.Hash(presented), expected))
            return Task.FromResult<IGenericResult>(GenericResult.Failure(SessionLog.CsrfRefused(
                logger, Name, request.Method, request.Path, configuration.Name, "the token does not match the session's")));

        SessionLog.CsrfPassed(logger, Name, request.Method, request.Path, configuration.Name);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }
}
