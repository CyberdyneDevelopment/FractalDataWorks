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

    /// <summary>Whether two issuer strings name the same issuer, ignoring a trailing slash on either.</summary>
    /// <param name="left">One issuer string.</param>
    /// <param name="right">The other.</param>
    /// <returns><see langword="true"/> when they name the same issuer.</returns>
    /// <remarks>
    /// The value is still never rewritten — a token keeps whatever the minter put in it and a binding
    /// keeps whatever was declared. What changes is that the two are compared as issuers rather than
    /// as bytes. The minter reads <c>auth.JwtTokenManager.Issuer</c> and the validator reads the
    /// declared <c>Authority</c>; each is free to carry or omit the trailing slash, and nothing makes
    /// them agree. Comparing ordinally turned that into 401 on every authenticated route twice in one
    /// day, from opposite directions — once with the slash on the declared side, once on the token
    /// side. Aligning the two strings fixes one direction and leaves the other live.
    /// </remarks>
    public static bool Matches(string? left, string? right)
        => string.Equals(Canonical(left), Canonical(right), StringComparison.Ordinal);

    /// <summary>Both spellings of an issuer, so an ordinal comparison performed elsewhere accepts either.</summary>
    /// <param name="issuer">The issuer as declared.</param>
    /// <returns>The issuer with and without its trailing slash.</returns>
    /// <remarks>
    /// For <c>TokenValidationParameters.ValidIssuers</c>, whose comparison is inside the JWT handler
    /// and ordinal. Supplying both spellings is how <see cref="Matches"/> is expressed to a comparison
    /// this code does not perform itself.
    /// </remarks>
    public static string[] Spellings(string issuer)
        => [Canonical(issuer), Canonical(issuer) + "/"];

    private static string Canonical(string? issuer)
        => issuer is null ? string.Empty : issuer.TrimEnd('/');
}
