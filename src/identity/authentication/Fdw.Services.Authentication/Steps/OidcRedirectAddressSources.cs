using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Authentication.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Where an OIDC authority's callback address comes from (FDW-5730): the redirect_uri the provider is asked to
/// send the person back to. An authority row names one in <c>RedirectAddressSource</c>; there is no default source,
/// and a row that names none is refused.
/// </summary>
/// <remarks>
/// <para>
/// <c>RequestHost</c> builds the callback on the scheme and host of the sign-in request, as the host's
/// forwarded-headers middleware reports them, and only when that origin is one the row lists. A sign-in on an A/B
/// slot's staging hostname then returns to that slot, and one on the public hostname to the live slot, though both
/// slots read one row. <c>FixedAddress</c> is the row's absolute RedirectUri, the same for every request.
/// </para>
/// <para>
/// The source generator emits <c>ByName</c>, <c>All</c> and <c>NotFound</c>.
/// </para>
/// </remarks>
[TypeCollection(typeof(OidcRedirectAddressSourceBase), typeof(OidcRedirectAddressSourceBase), typeof(OidcRedirectAddressSources))]
public abstract partial class OidcRedirectAddressSources : TypeCollectionBase<OidcRedirectAddressSourceBase, OidcRedirectAddressSourceBase>
{
    /// <summary>Lists the names an authority row may state, for messages that say what was expected.</summary>
    /// <returns>The names, comma separated.</returns>
    public static string KnownNames() => string.Join(", ", All().Select(static option => option.Name));

    /// <summary>
    /// Reads what an authority row states about its callback address: the source it names, checked by that source
    /// together with the RedirectUri and AllowedRedirectOrigins beside it.
    /// </summary>
    /// <param name="authority">The authority row's name, for the messages.</param>
    /// <param name="source">The row's RedirectAddressSource, as stated.</param>
    /// <param name="redirectUri">The row's RedirectUri, as stated.</param>
    /// <param name="allowedOrigins">The row's AllowedRedirectOrigins, as stated.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>What the row states, checked; or the refusal naming what is missing or wrong.</returns>
    public static IGenericResult<OidcRedirectAddress> Read(
        string authority, string? source, string? redirectUri, string? allowedOrigins, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.SourceNotStated(logger, authority, KnownNames()));
        }

        var chosen = ByName(source.Trim());
        return chosen.IsEmpty
            ? GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.SourceUnknown(logger, authority, source, KnownNames()))
            : chosen.Check(authority, redirectUri, allowedOrigins, logger);
    }
}
