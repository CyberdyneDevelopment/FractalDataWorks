using System;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// The callback address is the row's absolute RedirectUri (FDW-5730), the same for every request: how every
/// authority row worked before the source was named.
/// </summary>
/// <remarks>
/// A host that runs in more than one place behind more than one hostname (A/B slots) must not use it: every
/// copy sends the person back to the one address the row states. <see cref="RequestHostRedirectSource"/> is the
/// source for those.
/// </remarks>
[TypeOption(typeof(OidcRedirectAddressSources), "FixedAddress")]
public sealed class FixedAddressRedirectSource : OidcRedirectAddressSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="FixedAddressRedirectSource"/> class.</summary>
    public FixedAddressRedirectSource()
        : base("FixedAddress", "Fixed address", "The absolute http or https address the authority row states in RedirectUri, the same for every request")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<OidcRedirectAddress> CheckRow(string authority, string? redirectUri, string? allowedOrigins, ILogger logger)
    {
        if (!string.IsNullOrWhiteSpace(allowedOrigins))
        {
            return GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.OriginsNotRead(logger, authority, allowedOrigins));
        }

        return Uri.TryCreate(redirectUri, UriKind.Absolute, out var address) && OidcRedirectOrigins.IsHttp(address)
            ? GenericResult<OidcRedirectAddress>.Success(new OidcRedirectAddress { Source = Name, RedirectUri = address, AllowedOrigins = [] })
            : GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.FixedAddressInvalid(logger, authority, redirectUri ?? string.Empty));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The request is not read: the address is the row's whatever host the person came in on. The address was
    /// checked when the row was read; one that is not absolute did not come from this source's check.
    /// </remarks>
    protected override IGenericResult<Uri> ResolveAddress(IOidcAuthorityConfiguration configuration, HttpContext? context, ILogger logger)
        => configuration.RedirectUri.IsAbsoluteUri
            ? GenericResult<Uri>.Success(configuration.RedirectUri)
            : GenericResult<Uri>.Failure(RedirectAddressLog.FixedAddressInvalid(logger, configuration.Name, configuration.RedirectUri.OriginalString));
}
