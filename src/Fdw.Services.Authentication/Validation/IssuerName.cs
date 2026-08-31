using System;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// The one place an authority becomes the string an issuer actually puts in a token.
/// </summary>
/// <remarks>
/// A token's <c>iss</c> is compared ordinally in two places that have to agree: the selector matches
/// it against the binding to pick a scheme, and the scheme's <c>ValidIssuer</c> checks it again. If
/// only one of them normalises, a host declaring <c>https://auth.example</c> routes a token minted
/// against <c>https://auth.example/</c> to the right scheme and then refuses it there — which reads
/// as a signing problem and is not one.
/// </remarks>
internal static class IssuerName
{
    /// <summary>Reads a declared authority as the issuer string a token will carry.</summary>
    /// <param name="authority">The authority as declared.</param>
    /// <param name="serviceName">The entry this authority belongs to, for the failure.</param>
    /// <param name="log">The logger.</param>
    /// <returns>The normalised issuer, or a failure when the authority is not an absolute http(s) URI.</returns>
    public static IGenericResult<string> Read(string? authority, string serviceName, ILogger log)
        => Uri.TryCreate(authority, UriKind.Absolute, out var uri)
           && (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
               || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal))
            ? GenericResult<string>.Success(uri.AbsoluteUri)
            : GenericResult<string>.Failure(
                AuthenticationValidationLog.AuthorityNotAbsolute(log, serviceName, authority ?? string.Empty));
}
