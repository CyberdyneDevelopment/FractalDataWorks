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
/// it against the binding to pick a scheme, and the scheme's <c>ValidIssuer</c> checks it again.
/// Validated but otherwise passed through unchanged — never rewritten to <c>Uri.AbsoluteUri</c>, which
/// canonicalises a bare origin like <c>https://auth.example</c> into <c>https://auth.example/</c>. A
/// token is minted with whatever <c>auth.JwtTokenManager.Issuer</c> was configured as (also passed
/// through unchanged, by the same rule, in <c>JwtTokenIssuer</c>); rewriting only one side is what
/// produces the mismatch this class exists to prevent, not what fixes it.
/// </remarks>
internal static class IssuerName
{
    /// <summary>Reads a declared authority as the issuer string a token will carry.</summary>
    /// <param name="authority">The authority as declared.</param>
    /// <param name="serviceName">The entry this authority belongs to, for the failure.</param>
    /// <param name="log">The logger.</param>
    /// <returns>The authority unchanged, or a failure when it is not an absolute http(s) URI.</returns>
    public static IGenericResult<string> Read(string? authority, string serviceName, ILogger log)
        => Uri.TryCreate(authority, UriKind.Absolute, out var uri)
           && (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
               || string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.Ordinal))
            ? GenericResult<string>.Success(authority!)
            : GenericResult<string>.Failure(
                AuthenticationValidationLog.AuthorityNotAbsolute(log, serviceName, authority ?? string.Empty));
}
