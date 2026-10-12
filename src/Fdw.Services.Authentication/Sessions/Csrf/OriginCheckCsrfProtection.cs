using System;
using System.Linq;
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
/// Accepts a cookie-authenticated write only when its <c>Origin</c> is one the Session service lists.
/// </summary>
/// <remarks>
/// A browser sets <c>Origin</c> on every cross-origin request and on every same-origin request that
/// is not a GET or HEAD, and script cannot change it. Another site riding the cookie therefore
/// arrives naming itself. A write with no <c>Origin</c> is refused rather than waved through: a
/// browser app always sends one, and treating its absence as consent is the gap this closes.
/// </remarks>
[TypeOption(typeof(SessionCsrfProtectionTypes), "OriginCheck")]
public sealed class OriginCheckCsrfProtection : SessionCsrfProtectionBase
{
    /// <summary>Initializes a new instance of the <see cref="OriginCheckCsrfProtection"/> class.</summary>
    public OriginCheckCsrfProtection()
        : base("OriginCheck")
    {
    }

    /// <inheritdoc />
    public override IGenericResult ValidateConfiguration(ISessionAuthenticationConfiguration configuration, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (configuration.CsrfHeaderName is not null)
            return GenericResult.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, "CsrfHeaderName is set, but OriginCheck reads no header; set it only for SynchronizerToken"));

        if (configuration.AllowedOrigins is not { } allowed || Origins(allowed) is not { Length: > 0 } origins)
            return GenericResult.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, "OriginCheck lists no AllowedOrigins; name every origin the browser app is served from"));

        // An origin is scheme, host and optional port -- no path, no trailing slash. Anything else
        // can never equal what a browser sends, so the row would refuse every write it meant to allow.
        var malformed = origins.FirstOrDefault(o =>
            !Uri.TryCreate(o, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !string.Equals(uri.GetLeftPart(UriPartial.Authority), o, StringComparison.Ordinal));
        return malformed is null
            ? GenericResult.Success()
            : GenericResult.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, $"'{malformed}' in AllowedOrigins is not an origin (scheme://host[:port], nothing after it)"));
    }

    /// <inheritdoc />
    public override Task<IGenericResult> Verify(
        HttpRequest request, SessionRecord session, ISessionAuthenticationConfiguration configuration, ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(configuration);

        var origin = request.Headers.Origin.ToString();
        if (origin.Length == 0)
            return Task.FromResult<IGenericResult>(GenericResult.Failure(SessionLog.CsrfRefused(
                logger, Name, request.Method, request.Path, configuration.Name, "the request names no Origin")));

        if (configuration.AllowedOrigins is not { } allowed)
            return Task.FromResult<IGenericResult>(GenericResult.Failure(SessionLog.SessionServiceInvalid(
                logger, configuration.Name, "OriginCheck lists no AllowedOrigins; name every origin the browser app is served from")));

        if (!Origins(allowed).Contains(origin, StringComparer.Ordinal))
            return Task.FromResult<IGenericResult>(GenericResult.Failure(SessionLog.CsrfRefused(
                logger, Name, request.Method, request.Path, configuration.Name, $"Origin '{origin}' is not listed")));

        SessionLog.CsrfPassed(logger, Name, request.Method, request.Path, configuration.Name);
        return Task.FromResult<IGenericResult>(GenericResult.Success());
    }

    private static string[] Origins(string allowedOrigins)
        => allowedOrigins.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
}
