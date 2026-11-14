using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// The callback goes back to the host the person came in on (FDW-5730): the sign-in request's scheme and host, as
/// the host's forwarded-headers middleware reports them, joined to the callback path the row states, and only when
/// that origin is one the row lists.
/// </summary>
/// <remarks>
/// <para>
/// The row's RedirectUri gives the callback PATH, in either of two forms: the path alone
/// (<c>/api/v1/auth/oidc/callback</c>), or an absolute http or https address
/// (<c>https://app.example/api/v1/auth/oidc/callback</c>), of which only the path is taken. The absolute form exists
/// for A/B slots: both slots read one row in one database, and a build from before this source existed reads
/// RedirectUri as an absolute address and ignores the source and the list, so a row that keeps its absolute
/// RedirectUri serves the older build unchanged while this build on either slot returns to the hostname in use.
/// </para>
/// <para>
/// What is built is always the request's origin (checked against the list) joined to the stored path; the origin of
/// an absolute RedirectUri is never used, because a stored host would be one fixed address again, the fault this
/// source exists to remove. It must still be one of the listed origins, or the row is refused when it is read: the
/// stored address is where the older build on the other slot sends people, so it is a callback this row trusts,
/// and a row whose two statements of trusted callbacks disagree is a mistake to stop, not to run with. The path taken
/// from an absolute address obeys the same rules as one stated alone (a single leading '/', no query or fragment):
/// <c>https://app.example//other.example/cb</c> has the path <c>//other.example/cb</c>, which joined to an origin
/// would be read as another host, and is refused.
/// </para>
/// <para>
/// Both A/B slots of a host read one authority row. With this source a sign-in on the staging hostname is sent back
/// to the staging hostname, and one on the public hostname to the public hostname, so the session cookie lands on
/// the origin the person is using.
/// </para>
/// <para>
/// The request's host is the caller's to choose, so it is never trusted by itself: its origin must be one of the
/// row's AllowedRedirectOrigins, compared on scheme, host and port, or the sign-in is refused before the person is
/// sent to the provider. There is no wildcard and no default list. The address sent is built on the LISTED origin,
/// so it is spelled exactly as the provider registered it.
/// </para>
/// <para>
/// The scheme is <see cref="HttpRequest.Scheme"/>, which the forwarded-headers middleware sets from
/// X-Forwarded-Proto behind a proxy that ends TLS; the host is <see cref="HttpRequest.Host"/>, which a proxy that
/// passes the Host header through (Caddy does) leaves as the hostname the person used. A host whose middleware does
/// not run reports http, which is refused against an https list rather than sent.
/// </para>
/// </remarks>
[TypeOption(typeof(OidcRedirectAddressSources), "RequestHost")]
public sealed class RequestHostRedirectSource : OidcRedirectAddressSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="RequestHostRedirectSource"/> class.</summary>
    public RequestHostRedirectSource()
        : base("RequestHost", "Request host", "The sign-in request's own scheme and host, as forwarded headers report them, joined to the callback path the authority row states, and only when that origin is in the row's AllowedRedirectOrigins")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<OidcRedirectAddress> CheckRow(string authority, string? redirectUri, string? allowedOrigins, ILogger logger)
    {
        var path = CallbackPath(authority, redirectUri, logger);
        if (path.IsFailure || path.Value is null)
        {
            return path.ToNewResult<OidcRedirectAddress>();
        }

        var entries = OidcRedirectOrigins.Entries(allowedOrigins);
        if (entries.Length == 0)
        {
            return GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.OriginsMissing(logger, authority));
        }

        var origins = new List<Uri>(entries.Length);
        foreach (var entry in entries)
        {
            if (OidcRedirectOrigins.Origin(entry) is not { } origin)
            {
                return GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.OriginInvalid(logger, authority, entry));
            }

            origins.Add(origin);
        }

        if (StoredOrigin(redirectUri) is { } stored && OidcRedirectOrigins.Listed(origins, stored) is null)
        {
            return GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.StoredOriginNotListed(
                logger, authority, stored.GetLeftPart(UriPartial.Authority), string.Join(", ", entries)));
        }

        return GenericResult<OidcRedirectAddress>.Success(new OidcRedirectAddress
        {
            Source = Name,
            RedirectUri = new Uri(path.Value, UriKind.Relative),
            AllowedOrigins = origins,
        });
    }

    /// <inheritdoc/>
    protected override IGenericResult<Uri> ResolveAddress(IOidcAuthorityConfiguration configuration, HttpContext? context, ILogger logger)
    {
        if (context is null)
        {
            return GenericResult<Uri>.Failure(RedirectAddressLog.RequestUnavailable(logger, configuration.Name));
        }

        var requested = RequestOrigin(configuration.Name, context.Request, logger);
        if (requested.IsFailure)
        {
            return requested;
        }

        return OidcRedirectOrigins.Listed(configuration.AllowedRedirectOrigins, requested.Value!) is { } listed
            ? GenericResult<Uri>.Success(new Uri(listed, configuration.RedirectUri))
            : GenericResult<Uri>.Failure(RedirectAddressLog.OriginNotAllowed(logger, configuration.Name,
                requested.Value!.GetLeftPart(UriPartial.Authority),
                string.Join(", ", configuration.AllowedRedirectOrigins.Select(static origin => origin.GetLeftPart(UriPartial.Authority)))));
    }

    /// <summary>The request's origin, from its scheme and host as the forwarded-headers middleware left them.</summary>
    private static IGenericResult<Uri> RequestOrigin(string authority, HttpRequest request, ILogger logger)
    {
        if (!string.Equals(request.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return GenericResult<Uri>.Failure(RedirectAddressLog.SchemeUnusable(logger, authority, request.Scheme));
        }

        var host = request.Host;
        return host.HasValue
            && OidcRedirectOrigins.Origin(string.Concat(request.Scheme, Uri.SchemeDelimiter, host.Value)) is { } origin
                ? GenericResult<Uri>.Success(origin)
                : GenericResult<Uri>.Failure(RedirectAddressLog.HostUnusable(logger, authority, host.Value ?? string.Empty));
    }

    /// <summary>The address an absolute RedirectUri states, or <see langword="null"/> when the row states a path alone.</summary>
    /// <remarks>Read only after <see cref="CallbackPath"/> accepted the value, so an absolute one is http or https.</remarks>
    private static Uri? StoredOrigin(string? redirectUri)
        => redirectUri is { Length: > 0 } && redirectUri[0] != '/' && Uri.TryCreate(redirectUri, UriKind.Absolute, out var stored)
            ? stored
            : null;

    /// <summary>
    /// The callback path the row's RedirectUri gives: the value itself when it is a path, or the path of an absolute
    /// http or https address (whose host is ignored; see the remarks on this class). Either way the path must pass
    /// <see cref="IsCallbackPath"/>.
    /// </summary>
    private static IGenericResult<string> CallbackPath(string authority, string? redirectUri, ILogger logger)
    {
        if (redirectUri is { Length: > 0 } && redirectUri[0] == '/')
        {
            return IsCallbackPath(redirectUri)
                ? GenericResult<string>.Success(redirectUri)
                : GenericResult<string>.Failure(RedirectAddressLog.CallbackPathInvalid(logger, authority, redirectUri));
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var address) || !OidcRedirectOrigins.IsHttp(address))
        {
            return GenericResult<string>.Failure(RedirectAddressLog.RedirectUriUnreadable(logger, authority, redirectUri ?? string.Empty));
        }

        return address.Query.Length == 0 && address.Fragment.Length == 0 && IsCallbackPath(address.AbsolutePath)
            ? GenericResult<string>.Success(address.AbsolutePath)
            : GenericResult<string>.Failure(RedirectAddressLog.CallbackPathInvalid(logger, authority, address.OriginalString));
    }

    /// <summary>
    /// Whether the row's RedirectUri is a callback path: a single leading '/', no query or fragment. A leading '//'
    /// is refused: joined to an origin it is read as another host.
    /// </summary>
    private static bool IsCallbackPath([NotNullWhen(true)] string? value)
        => value is { Length: > 1 }
            && value[0] == '/'
            && value[1] != '/'
            && value.IndexOfAny(['?', '#', '\\']) < 0
            && Uri.IsWellFormedUriString(value, UriKind.Relative);
}
