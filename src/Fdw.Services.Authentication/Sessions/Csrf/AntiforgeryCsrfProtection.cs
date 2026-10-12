using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Sessions.Csrf;

/// <summary>
/// Accepts a cookie-authenticated write only when it passes ASP.NET Core's own antiforgery check —
/// the double-submit-cookie synchronizer pattern the framework ships and maintains, rather than a
/// hand-rolled equivalent (FDW-945).
/// </summary>
/// <remarks>
/// Unlike <see cref="OriginCheckCsrfProtection"/>, nothing here depends on a hand-maintained list of
/// trusted hostnames: <c>IAntiforgery</c> mints a token pair and verifies the caller holds both
/// halves, so there is no allowlist to drift out of date. A TypeOption is constructed parameterlessly
/// by the source-generated registration, never through DI, so <c>IAntiforgery</c> is resolved per-call
/// from <see cref="HttpContext.RequestServices"/> — the request's own DI scope — rather than
/// constructor-injected. This reads the ambient container; it does not store one, so it is not the
/// service-locator anti-pattern that rule forbids.
/// </remarks>
[TypeOption(typeof(SessionCsrfProtectionTypes), "Antiforgery")]
public sealed class AntiforgeryCsrfProtection : SessionCsrfProtectionBase, ICsrfTokenIssuer
{
    /// <summary>Initializes a new instance of the <see cref="AntiforgeryCsrfProtection"/> class.</summary>
    public AntiforgeryCsrfProtection()
        : base("Antiforgery")
    {
    }

    /// <inheritdoc />
    public override IGenericResult ValidateConfiguration(ISessionAuthenticationConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.AllowedOrigins is not null
            ? GenericResult.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, "AllowedOrigins is set, but Antiforgery checks no origin; set it only for OriginCheck"))
            : GenericResult.Success();
    }

    /// <inheritdoc />
    public override async Task<IGenericResult> Verify(
        HttpRequest request, SessionRecord session, ISessionAuthenticationConfiguration configuration, ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(configuration);

        var antiforgery = request.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            await antiforgery.ValidateRequestAsync(request.HttpContext).ConfigureAwait(false);
        }
        catch (AntiforgeryValidationException ex)
        {
            return GenericResult.Failure(SessionLog.CsrfRefused(
                logger, Name, request.Method, request.Path, configuration.Name, ex.Message));
        }

        SessionLog.CsrfPassed(logger, Name, request.Method, request.Path, configuration.Name);
        return GenericResult.Success();
    }

    /// <inheritdoc />
    public IGenericResult<string> IssueToken(HttpContext context, Guid sessionId, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(context);

        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        var tokens = antiforgery.GetAndStoreTokens(context);
        if (tokens.RequestToken is not { } token)
            return GenericResult<string>.Failure(SessionLog.SessionWriteFailed(logger, "issue an antiforgery CSRF token"));

        SessionLog.CsrfTokenIssued(logger, sessionId);
        return GenericResult<string>.Success(token);
    }
}
