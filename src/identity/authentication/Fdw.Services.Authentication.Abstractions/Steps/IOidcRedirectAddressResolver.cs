using System;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Gives the callback address an OIDC sign-in asks the provider to send the person back to (FDW-5730).
/// </summary>
/// <remarks>
/// Where it comes from is the authority row's <see cref="IOidcAuthorityConfiguration.RedirectAddressSource"/>: the
/// sign-in request's own host checked against the row's list, or the row's fixed address. A step never sends
/// <see cref="IOidcAuthorityConfiguration.RedirectUri"/> as it stands, because for a request-host row it is only the
/// path.
/// </remarks>
public interface IOidcRedirectAddressResolver
{
    /// <summary>Gives the absolute callback address for the sign-in request in hand.</summary>
    /// <param name="configuration">The authority, as its row was read and checked.</param>
    /// <returns>The callback address, or the refusal naming why there is none (each reason its own message).</returns>
    IGenericResult<Uri> Resolve(IOidcAuthorityConfiguration configuration);
}
